var NotifyControl = (function () {
    function NotifyControl() {
        this._text = "";
    }
    Object.defineProperty(NotifyControl.prototype, "text", {
        get: function () {
            return this._text;
        },
        set: function (v) {
            this._text = v;
        },
        enumerable: true,
        configurable: true
    });
    return NotifyControl;
})();
