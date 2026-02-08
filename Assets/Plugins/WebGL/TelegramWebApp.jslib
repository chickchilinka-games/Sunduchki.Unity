mergeInto(LibraryManager.library, {
  TelegramWebAppGetInitData: function () {
    var data = "";
    try {
      if (window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initData) {
        data = window.Telegram.WebApp.initData;
      }
    } catch (e) {
      data = "";
    }
    var length = lengthBytesUTF8(data) + 1;
    var buffer = _malloc(length);
    stringToUTF8(data, buffer, length);
    return buffer;
  }
});
