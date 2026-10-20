// 크로마 프론티어 온라인: Firebase 로그인(Google·게스트), 클라우드 저장, PvP 방(1:1 / 2:2)과 수 주고받기.
// Unity 는 Plugins/WebGL/Net.jslib 를 거쳐 window.CFNet 의 함수를 부르고, 결과는 CFNet.poll() 로 메시지(JSON 문자열)를
// 하나씩 가져간다 (Client/Online.cs). 주소에 ?mock 을 붙이면 Firebase 대신 이 브라우저 안의 가짜 DB 를 쓴다
// (localStorage 를 탭끼리 공유하므로 탭 두 개로 대전을 시험할 수 있다). ?tab 이면 로그인을 탭마다 따로 유지한다.
//
// DB 구조 (Realtime Database)
//   users/{uid}/name            대전에 보이는 이름
//   users/{uid}/save            { at, data } — data 는 localStorage 의 cf.* 값 전부(JSON 문자열)
//   rooms/{id}                  { title, size(팀 인원 1|2), host, status(wait|play|done), map, seed, created,
//                                 seats/{0..3}: { uid, name, ready, left, deck } }  짝수 자리 = 1팀, 홀수 자리 = 2팀
//   moves/{id}/{n}              n 번째 수 (Core PvpMove 문자열). 한 번 쓰면 못 바꾼다 (보안 규칙)
(function () {
  'use strict';
  var SDK = 'https://www.gstatic.com/firebasejs/10.14.1/';
  var params = new URLSearchParams(location.search);
  var queue = [];
  var db = null, auth = null, user = null, mock = params.has('mock');
  var ready = false, failed = false;

  function emit(msg) { queue.push(JSON.stringify(msg)); }
  function errText(e) { return String((e && (e.code || e.message)) || e); }
  function fail(e) { console.warn('[CFNet]', e); emit({ t: 'err', err: errText(e) }); }
  function rand(n) { return Math.floor(Math.random() * n); }

  function loadScript(src) {
    return new Promise(function (resolve, reject) {
      var s = document.createElement('script');
      s.src = src;
      s.onload = resolve;
      s.onerror = function () { reject(new Error('load ' + src)); };
      document.head.appendChild(s);
    });
  }

  // ------------------------------------------------------------------ DB: Firebase / 가짜

  function firebaseDb(fdb) {
    return {
      get: function (p) { return fdb.ref(p).once('value').then(function (s) { return s.val(); }); },
      set: function (p, v) { return fdb.ref(p).set(v); },
      update: function (p, v) { return fdb.ref(p).update(v); },
      remove: function (p) { return fdb.ref(p).remove(); },
      key: function () { return fdb.ref('rooms').push().key; },
      txn: function (p, fn) {
        return fdb.ref(p).transaction(fn, undefined, false).then(function (r) { return { committed: r.committed, value: r.snapshot.val() }; });
      },
      watch: function (p, cb) {
        var ref = fdb.ref(p);
        var h = ref.on('value', function (s) { cb(s.val()); }, fail);
        return function () { ref.off('value', h); };
      },
      onDisconnect: function (p) {
        var od = fdb.ref(p).onDisconnect();
        return { set: function (v) { return od.set(v); }, remove: function () { return od.remove(); }, cancel: function () { return od.cancel(); } };
      },
    };
  }

  // 가짜 DB: localStorage 의 JSON 트리 하나. 다른 탭이 바꾸면 storage 이벤트로 알림을 받는다.
  function mockDb() {
    var KEY = 'cfmock.db', listeners = [];
    function load() { try { return JSON.parse(localStorage.getItem(KEY)) || {}; } catch (e) { return {}; } }
    function parts(p) { return p.split('/').filter(Boolean); }
    function getAt(root, p) {
      var o = root;
      parts(p).forEach(function (k) { o = o && typeof o === 'object' ? o[k] : undefined; });
      return o === undefined ? null : o;
    }
    function prune(o) {
      if (!o || typeof o !== 'object') return o;
      Object.keys(o).forEach(function (k) {
        o[k] = prune(o[k]);
        if (o[k] === null || o[k] === undefined || (typeof o[k] === 'object' && !Object.keys(o[k]).length)) delete o[k];
      });
      return o;
    }
    function setAt(root, p, v) {
      var ks = parts(p);
      if (!ks.length) return v === null ? {} : JSON.parse(JSON.stringify(v));
      var o = root;
      for (var i = 0; i < ks.length - 1; i++) {
        if (!o[ks[i]] || typeof o[ks[i]] !== 'object') o[ks[i]] = {};
        o = o[ks[i]];
      }
      if (v === null || v === undefined) delete o[ks[ks.length - 1]];
      else o[ks[ks.length - 1]] = JSON.parse(JSON.stringify(v));
      return root;
    }
    function fire() {
      var root = load();
      listeners.slice().forEach(function (l) {
        var v = JSON.stringify(getAt(root, l.path));
        if (v === l.last) return;
        l.last = v;
        setTimeout(function () { if (l.on) l.cb(JSON.parse(v)); }, 30);
      });
    }
    function save(root) { localStorage.setItem(KEY, JSON.stringify(prune(root))); fire(); }
    window.addEventListener('storage', function (e) { if (e.key === KEY) fire(); });
    var api = {
      get: function (p) { return Promise.resolve(getAt(load(), p)); },
      set: function (p, v) { save(setAt(load(), p, v)); return Promise.resolve(); },
      update: function (p, obj) {
        var root = load();
        Object.keys(obj).forEach(function (k) { root = setAt(root, p + '/' + k, obj[k]); });
        save(root);
        return Promise.resolve();
      },
      remove: function (p) { return api.set(p, null); },
      key: function () { return 'm' + Date.now().toString(36) + rand(1e6).toString(36); },
      txn: function (p, fn) {
        var root = load(), cur = getAt(root, p);
        var next = fn(cur === null ? null : JSON.parse(JSON.stringify(cur)));
        if (next === undefined) return Promise.resolve({ committed: false, value: cur });
        save(setAt(root, p, next));
        return Promise.resolve({ committed: true, value: next });
      },
      watch: function (p, cb) {
        var l = { path: p, cb: cb, last: undefined, on: true };
        listeners.push(l);
        fire();
        return function () { l.on = false; listeners.splice(listeners.indexOf(l), 1); };
      },
      onDisconnect: function () {
        var none = function () { return Promise.resolve(); };
        return { set: none, remove: none, cancel: none };
      },
    };
    return api;
  }

  // ------------------------------------------------------------------ 로그인: Firebase / 가짜

  function userOf(u) {
    return { uid: u.uid, provider: u.isAnonymous ? 'guest' : 'google', name: u.displayName || '' };
  }

  function firebaseAuth(fa) {
    var provider = function () { return new firebase.auth.GoogleAuthProvider(); };
    if (params.has('tab')) fa.setPersistence(firebase.auth.Auth.Persistence.SESSION);
    return {
      onChange: function (cb) { fa.onAuthStateChanged(function (u) { cb(u ? userOf(u) : null); }); },
      google: function () { return fa.signInWithPopup(provider()); },
      guest: function () { return fa.signInAnonymously(); },
      link: function () { return fa.currentUser.linkWithPopup(provider()).then(function (r) { return userOf(r.user); }); },
      signOut: function () { return fa.signOut(); },
    };
  }

  // 가짜 로그인: 탭마다(sessionStorage) 따로. Google 도 게스트처럼 바로 만들어진다.
  function mockAuth() {
    var KEY = 'cfmock.user', cb = null;
    function cur() { try { return JSON.parse(sessionStorage.getItem(KEY)); } catch (e) { return null; } }
    function put(u) {
      if (u) sessionStorage.setItem(KEY, JSON.stringify(u)); else sessionStorage.removeItem(KEY);
      setTimeout(function () { cb && cb(u); }, 50);
      return Promise.resolve(u);
    }
    function make(provider) {
      var id = rand(1e9).toString(36);
      return { uid: (provider === 'google' ? 'g-' : 'a-') + id, provider: provider, name: provider === 'google' ? '테스터' + id.slice(0, 3) : '' };
    }
    return {
      onChange: function (f) { cb = f; setTimeout(function () { cb(cur()); }, 50); },
      google: function () { return put(make('google')); },
      guest: function () { return put(make('guest')); },
      link: function () { var u = cur(); u.provider = 'google'; u.name = u.name || '테스터'; return put(u); },
      signOut: function () { return put(null); },
    };
  }

  // ------------------------------------------------------------------ 클라우드 저장

  var SYNC_UID = 'cf.sync.uid', SYNC_AT = 'cf.sync.at';
  var syncing = false, synced = false, dirty = false, uploadTimer = 0, pendingRemote = null;

  function localKeys() {
    var keys = [];
    for (var i = 0; i < localStorage.length; i++) {
      var k = localStorage.key(i);
      if (k && k.indexOf('cf.') === 0 && k.indexOf('cf.sync.') !== 0 && k.indexOf('cf.local.') !== 0) keys.push(k);
    }
    return keys;
  }
  function localData() {
    var o = {};
    localKeys().forEach(function (k) { o[k] = localStorage.getItem(k); });
    return o;
  }
  function hasProgress(d) {
    return d['cf.tutorial'] === '1' || (+d['cf.gems'] || 0) > 0 || (+d['cf.unlocked'] || 1) > 1;
  }
  function summary(d) {
    var stars = 0;
    Object.keys(d).forEach(function (k) {
      if (k.indexOf('cf.stars') === 0) String(d[k]).split(',').forEach(function (v) { stars += +v || 0; });
    });
    return '스테이지 별 ' + stars + ' · 결정 ' + (+d['cf.gems'] || 0) + ' · 해금 ' + (+d['cf.unlocked'] || 1) + '스테이지';
  }
  function clearLocal() { localKeys().forEach(function (k) { localStorage.removeItem(k); }); }
  function applyRemote(remote) {
    clearLocal();
    var d = {};
    try { d = JSON.parse(remote.data) || {}; } catch (e) { }
    Object.keys(d).forEach(function (k) { if (k.indexOf('cf.') === 0) localStorage.setItem(k, d[k]); });
    localStorage.setItem(SYNC_UID, user.uid);
    localStorage.setItem(SYNC_AT, String(remote.at || Date.now()));
    emit({ t: 'reload' });
  }
  function upload() {
    if (!user || !synced) return Promise.resolve();
    clearTimeout(uploadTimer);
    dirty = false;
    var at = +localStorage.getItem(SYNC_AT) || Date.now();
    localStorage.setItem(SYNC_UID, user.uid);
    return db.set('users/' + user.uid + '/save', { at: at, data: JSON.stringify(localData()) })
      .then(function () { emit({ t: 'saved' }); })
      .catch(function (e) { dirty = true; fail(e); });
  }
  function finishSync() {
    syncing = false;
    synced = true;
    emit({ t: 'synced' });
  }

  // 로그인 직후: 계정에 저장된 기록과 이 기기의 기록을 맞춘다.
  function pull() {
    syncing = true;
    synced = false;
    db.get('users/' + user.uid + '/save').then(function (remote) {
      var owner = localStorage.getItem(SYNC_UID), localAt = +localStorage.getItem(SYNC_AT) || 0, local = localData();
      if (!remote || !remote.data) {
        // 계정에 기록이 없다: 이 기기의 기록을 올린다. 다른 계정의 기록이 남아 있으면 새로 시작.
        if (owner && owner !== user.uid) { clearLocal(); emit({ t: 'reload' }); }
        finishSync();
        localStorage.setItem(SYNC_AT, String(Date.now()));
        return upload();
      }
      if (owner === user.uid) {
        if ((remote.at || 0) > localAt) applyRemote(remote);
        finishSync();
        if (localAt > (remote.at || 0)) return upload();
        return;
      }
      if (!owner && hasProgress(local)) {
        // 로그인 전에 이 기기에서 하던 기록과 계정 기록이 둘 다 있다: 고르게 한다
        pendingRemote = remote;
        emit({ t: 'conflict', localSummary: summary(local), remoteSummary: summary(JSON.parse(remote.data) || {}) });
        return;
      }
      applyRemote(remote);
      finishSync();
    }).catch(function (e) { syncing = false; fail(e); emit({ t: 'synced' }); });
  }

  function onAuth(u) {
    user = u;
    synced = false;
    if (!u) {
      emit({ t: 'auth', uid: '' });
      return;
    }
    db.get('users/' + u.uid + '/name').then(function (name) {
      if (!name) {
        name = (u.name || '게스트' + (1000 + rand(9000))).slice(0, 12);
        db.set('users/' + u.uid + '/name', name).catch(fail);
      }
      user.name = name;
      emit({ t: 'auth', uid: u.uid, name: name, provider: u.provider });
      pull();
    }).catch(function (e) {
      fail(e);
      emit({ t: 'auth', uid: u.uid, name: u.name || '플레이어', provider: u.provider });
      pull();
    });
  }

  // LocalStorage.jslib 가 cf.* 를 바꿀 때마다 부른다
  window.CFNetDirty = function (key) {
    if (!key || key.indexOf('cf.sync.') === 0 || key.indexOf('cf.local.') === 0) return;
    localStorage.setItem(SYNC_AT, String(Date.now()));
    if (!user || !synced) return;
    dirty = true;
    clearTimeout(uploadTimer);
    uploadTimer = setTimeout(upload, 2500);
  };
  document.addEventListener('visibilitychange', function () { if (document.hidden && dirty) upload(); });
  window.addEventListener('pagehide', function () { if (dirty) upload(); });

  // ------------------------------------------------------------------ 방

  var roomsOff = null, roomOff = null, movesOff = null;
  var roomId = null, room = null, mySeat = -1, playing = false, lastMoves = [];
  var disconnect = null;

  function seatsOf(r) {
    var n = (r.size || 1) * 2, list = [];
    for (var i = 0; i < n; i++) {
      var s = r.seats && r.seats[i];
      list.push(s ? { uid: s.uid || '', name: s.name || '', ready: !!s.ready, left: !!s.left, deck: s.deck || '' } : { uid: '', name: '', ready: false, left: false, deck: '' });
    }
    return list;
  }
  function info(id, r) {
    return { id: id, title: r.title || '', size: r.size || 1, host: r.host || '', status: r.status || 'wait', map: r.map || '', seed: r.seed || 0, created: r.created || 0, seats: seatsOf(r) };
  }
  function findSeat(r, uid) {
    var n = (r.size || 1) * 2;
    for (var i = 0; i < n; i++) if (r.seats && r.seats[i] && r.seats[i].uid === uid) return i;
    return -1;
  }
  function setDisconnect(path, value) {
    if (disconnect) disconnect.cancel().catch(function () { });
    disconnect = db.onDisconnect(path);
    (value === null ? disconnect.remove() : disconnect.set(value)).catch(fail);
  }
  function dropRoom() {
    if (roomOff) roomOff();
    if (movesOff) movesOff();
    if (disconnect) disconnect.cancel().catch(function () { });
    roomOff = movesOff = disconnect = null;
    roomId = room = null;
    mySeat = -1;
    playing = false;
    lastMoves = [];
  }

  function enterRoom(id) {
    dropRoom();
    roomId = id;
    roomOff = db.watch('rooms/' + id, function (r) {
      if (id !== roomId) return;
      if (!r) {
        dropRoom();
        emit({ t: 'roomGone' });
        return;
      }
      room = r;
      var seat = findSeat(r, user.uid);
      if (seat < 0 && r.status === 'wait') {
        dropRoom();
        emit({ t: 'roomGone' });
        return;
      }
      if (seat !== mySeat && r.status === 'wait') {
        mySeat = seat;
        // 기다리는 동안 끊기면: 방장은 방을 지우고, 나머지는 자리만 비운다
        setDisconnect(r.host === user.uid ? 'rooms/' + id : 'rooms/' + id + '/seats/' + seat, null);
      }
      mySeat = seat;
      if (r.status !== 'wait' && !playing && seat >= 0) {
        playing = true;
        // 판이 시작되면 끊겼을 때 '나감'으로 표시한다 (방은 남겨 둠)
        setDisconnect('rooms/' + id + '/seats/' + seat + '/left', true);
      }
      emit({ t: 'room', room: info(id, r), n: mySeat });
    });
    movesOff = db.watch('moves/' + id, function (m) {
      if (id !== roomId || !m) return;
      for (var i = 0; m[i] !== undefined && m[i] !== null; i++) {
        if (lastMoves[i] === m[i]) continue;
        lastMoves[i] = m[i];
        emit({ t: 'move', n: i, move: String(m[i]) });
      }
    });
  }

  function purgeOld(rooms) {
    var now = Date.now();
    Object.keys(rooms).forEach(function (id) {
      var r = rooms[id];
      if (r && (r.created || 0) < now - 3 * 3600 * 1000) {
        db.remove('rooms/' + id).catch(function () { });
        db.remove('moves/' + id).catch(function () { });
      }
    });
  }

  // ------------------------------------------------------------------ Unity 에서 부르는 함수

  var busyUntil = 0;
  function once(fn) {
    // 버튼을 누를 때(포인터를 뗄 때 실행되게 걸어 둠)와 Unity 가 클릭을 받을 때 둘 다 부르므로 한 번만 실행
    if (Date.now() < busyUntil) return;
    busyUntil = Date.now() + 1500;
    fn();
  }
  var actions = {
    google: function () { auth.google().catch(fail); },
    guest: function () { auth.guest().catch(fail); },
    link: function () {
      auth.link().then(function (u) { user.provider = u.provider; emit({ t: 'auth', uid: user.uid, name: user.name, provider: 'google' }); })
        .catch(fail);
    },
    name: function () {
      if (!user) return;
      var v = window.prompt('대전에 보일 이름 (12자까지)', user.name || '');
      if (v === null) return;
      v = v.replace(/[<>]/g, '').trim().slice(0, 12);
      if (!v) return;
      user.name = v;
      db.set('users/' + user.uid + '/name', v).catch(fail);
      emit({ t: 'auth', uid: user.uid, name: v, provider: user.provider });
    },
  };

  window.CFNet = {
    poll: function () { return queue.length ? queue.shift() : null; },
    available: function () { return true; },

    init: function (configJson) {
      if (ready || failed) return;
      if (mock) {
        db = mockDb();
        auth = mockAuth();
        ready = true;
        emit({ t: 'ready', provider: 'mock' });
        auth.onChange(onAuth);
        return;
      }
      loadScript(SDK + 'firebase-app-compat.js')
        .then(function () { return Promise.all([loadScript(SDK + 'firebase-auth-compat.js'), loadScript(SDK + 'firebase-database-compat.js')]); })
        .then(function () {
          firebase.initializeApp(JSON.parse(configJson));
          db = firebaseDb(firebase.database());
          auth = firebaseAuth(firebase.auth());
          ready = true;
          emit({ t: 'ready' });
          auth.onChange(onAuth);
        })
        .catch(function (e) { failed = true; fail(e); emit({ t: 'offline' }); });
    },

    // 버튼을 누르는 순간 걸어 두면, 포인터를 떼는 브라우저 이벤트 안에서 실행된다 (팝업 차단을 피함)
    arm: function (action) {
      if (!ready || !actions[action]) return;
      var h = function () { window.removeEventListener('pointerup', h, true); once(actions[action]); };
      window.addEventListener('pointerup', h, true);
      setTimeout(function () { window.removeEventListener('pointerup', h, true); }, 3000);
    },
    run: function (action) { if (ready && actions[action]) once(actions[action]); },

    signOut: function () {
      if (!ready || !user) return;
      upload().then(function () {
        clearLocal();
        localStorage.removeItem(SYNC_UID);
        localStorage.removeItem(SYNC_AT);
        emit({ t: 'reload' });
        return auth.signOut();
      }).catch(fail);
    },

    resolve: function (choice) {
      if (!pendingRemote) return;
      var remote = pendingRemote;
      pendingRemote = null;
      if (choice === 'remote') applyRemote(remote);
      finishSync();
      if (choice !== 'remote') {
        localStorage.setItem(SYNC_AT, String(Date.now()));
        upload();
      }
    },

    saveNow: function () { if (dirty) upload(); },

    watchRooms: function (on) {
      if (roomsOff) roomsOff();
      roomsOff = null;
      if (!on || !ready) return;
      roomsOff = db.watch('rooms', function (all) {
        all = all || {};
        purgeOld(all);
        var list = [];
        Object.keys(all).forEach(function (id) { if (all[id] && all[id].status === 'wait') list.push(info(id, all[id])); });
        list.sort(function (a, b) { return b.created - a.created; });
        emit({ t: 'rooms', rooms: list.slice(0, 30) });
      });
    },

    createRoom: function (title, size, map, deck) {
      if (!user) return;
      size = +size === 2 ? 2 : 1;
      var id = db.key();
      var r = { title: title, size: size, host: user.uid, status: 'wait', map: map, seed: 0, created: Date.now(), seats: {} };
      r.seats[0] = { uid: user.uid, name: user.name, ready: true, left: false, deck: deck };
      db.set('rooms/' + id, r).then(function () { enterRoom(id); }).catch(fail);
    },

    joinRoom: function (id, deck) {
      if (!user) return;
      var seat = -1;
      db.txn('rooms/' + id, function (r) {
        if (!r) return r; // 캐시가 비었으면 서버 값으로 다시 불린다
        if (r.status !== 'wait') return undefined;
        var n = (r.size || 1) * 2;
        r.seats = r.seats || {};
        if (findSeat(r, user.uid) >= 0) { seat = findSeat(r, user.uid); return r; }
        for (var i = 0; i < n; i++) {
          if (!r.seats[i]) {
            r.seats[i] = { uid: user.uid, name: user.name, ready: false, left: false, deck: deck };
            seat = i;
            return r;
          }
        }
        return undefined;
      }).then(function (res) {
        if (res.committed && seat >= 0) enterRoom(id);
        else emit({ t: 'err', err: 'room-full' });
      }).catch(fail);
    },

    moveSeat: function (to) {
      if (!roomId || !user) return;
      to = +to;
      db.txn('rooms/' + roomId, function (r) {
        if (!r) return r;
        if (r.status !== 'wait' || !r.seats || r.seats[to]) return undefined;
        var from = findSeat(r, user.uid);
        if (from < 0) return undefined;
        r.seats[to] = r.seats[from];
        r.seats[from] = null;
        return r;
      }).catch(fail);
    },

    setReady: function (on) {
      if (!roomId || mySeat < 0) return;
      db.set('rooms/' + roomId + '/seats/' + mySeat + '/ready', !!on).catch(fail);
    },

    setMap: function (map) {
      if (!roomId || !room || room.host !== user.uid) return;
      db.set('rooms/' + roomId + '/map', map).catch(fail);
    },

    setDeck: function (deck) {
      if (!roomId || mySeat < 0) return;
      db.set('rooms/' + roomId + '/seats/' + mySeat + '/deck', deck).catch(fail);
    },

    start: function () {
      if (!roomId || !room || room.host !== user.uid) return;
      db.txn('rooms/' + roomId, function (r) {
        if (!r) return r;
        if (r.status !== 'wait') return undefined;
        var n = (r.size || 1) * 2;
        for (var i = 0; i < n; i++) if (!r.seats || !r.seats[i] || !(r.seats[i].ready || r.seats[i].uid === r.host)) return undefined;
        r.status = 'play';
        r.seed = 1 + rand(2000000000);
        r.started = Date.now();
        return r;
      }).then(function (res) { if (!res.committed) emit({ t: 'err', err: 'not-ready' }); }).catch(fail);
    },

    sendMove: function (n, move, onlyIfEmpty) {
      if (!roomId) return;
      var p = 'moves/' + roomId + '/' + n;
      if (onlyIfEmpty) db.txn(p, function (cur) { return cur === null ? move : undefined; }).catch(fail);
      else db.set(p, move).catch(fail);
    },

    finish: function () {
      if (!roomId || !room || room.host !== user.uid) return;
      db.set('rooms/' + roomId + '/status', 'done').catch(function () { });
    },

    leaveRoom: function () {
      if (!roomId || !user) { dropRoom(); return; }
      var id = roomId, r = room, seat = mySeat;
      dropRoom();
      if (!r) return;
      if (r.status === 'wait') {
        if (r.host === user.uid) {
          db.remove('rooms/' + id).catch(fail);
          db.remove('moves/' + id).catch(function () { });
        } else if (seat >= 0) db.remove('rooms/' + id + '/seats/' + seat).catch(fail);
        return;
      }
      // 판 중이거나 끝난 방: 나감으로 표시하고, 모두 나갔으면 방을 지운다
      if (seat < 0) return;
      db.set('rooms/' + id + '/seats/' + seat + '/left', true)
        .then(function () { return db.get('rooms/' + id); })
        .then(function (cur) {
          var n = cur ? (cur.size || 1) * 2 : 0;
          for (var i = 0; i < n; i++) if (cur.seats && cur.seats[i] && !cur.seats[i].left) return;
          db.remove('rooms/' + id).catch(function () { });
          db.remove('moves/' + id).catch(function () { });
        }).catch(fail);
    },
  };
})();
