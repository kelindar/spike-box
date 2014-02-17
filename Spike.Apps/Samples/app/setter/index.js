var Setter = (function () {
    function Setter() {
        this._name = " ";
    }
    Object.defineProperty(Setter.prototype, "name", {
        get: function () {
            return this._name;
        },
        set: function (v) {
            this._name = v;
        },
        enumerable: true,
        configurable: true
    });
    return Setter;
})();
