// 온라인(로그인·클라우드 저장·PvP) 다리 (Client/Online.cs). 실제 일은 WebGL 템플릿의 net.js(window.CFNet)가 한다.
// net.js 가 없으면(다른 템플릿 등) 모두 아무 일도 안 하고, CF_NetAvailable 이 0 을 돌려준다.
mergeInto(LibraryManager.library, {
  CF_NetAvailable: function () { return window.CFNet ? 1 : 0; },

  CF_NetCall: function (name, a, b, c, d) {
    if (!window.CFNet) return;
    var f = window.CFNet[UTF8ToString(name)];
    if (typeof f !== 'function') return;
    try { f(UTF8ToString(a), UTF8ToString(b), UTF8ToString(c), UTF8ToString(d)); } catch (e) { console.warn('[CFNet]', e); }
  },

  CF_NetPoll: function () {
    var msg = window.CFNet ? window.CFNet.poll() : null;
    if (msg === null) return 0;
    var size = lengthBytesUTF8(msg) + 1;
    var buffer = _malloc(size);
    stringToUTF8(msg, buffer, size);
    return buffer;
  },
});
