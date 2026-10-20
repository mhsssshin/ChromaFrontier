// 브라우저 localStorage 를 C# 에서 쓰기 위한 다리 (Client/LocalStore.cs).
// 저장은 즉시(동기) 일어나서, 탭을 바로 닫아도 진행이 남는다.
mergeInto(LibraryManager.library, {
  CF_StorageGet: function (key) {
    var value = null;
    try { value = window.localStorage.getItem(UTF8ToString(key)); } catch (e) { console.warn("[LocalStore]", e); }
    if (value === null) return 0;
    var size = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(size);
    stringToUTF8(value, buffer, size);
    return buffer;
  },

  CF_StorageSet: function (key, value) {
    var k = UTF8ToString(key);
    try { window.localStorage.setItem(k, UTF8ToString(value)); } catch (e) { console.warn("[LocalStore]", e); }
    if (window.CFNetDirty) window.CFNetDirty(k); // 로그인했으면 클라우드에 올린다 (net.js)
  },

  /// prefix 로 시작하는 키를 모두 지운다.
  CF_StorageClear: function (prefix) {
    try {
      var p = UTF8ToString(prefix), keys = [];
      for (var i = 0; i < window.localStorage.length; i++) {
        var k = window.localStorage.key(i);
        if (k && k.indexOf(p) === 0) keys.push(k);
      }
      keys.forEach(function (k) { window.localStorage.removeItem(k); });
      if (window.CFNetDirty && keys.length) window.CFNetDirty(keys[0]);
    } catch (e) { console.warn("[LocalStore]", e); }
  },
});
