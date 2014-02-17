var IconControl = (function () {
    function IconControl() {
        this._type = "";
    }
    Object.defineProperty(IconControl.prototype, "type", {
        get: function () {
            return this._type;
        },
        set: function (v) {
            this._type = v;
        },
        enumerable: true,
        configurable: true
    });
    return IconControl;
})();
