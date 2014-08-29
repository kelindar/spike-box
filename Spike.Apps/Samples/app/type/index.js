var TypeDemo = (function () {
    function TypeDemo() {
        this._info = {
            text: ""
        };
    }
    Object.defineProperty(TypeDemo.prototype, "info", {
        get: function () {
            return this._info;
        },
        set: function (v) {
            this._info = v;
        },
        enumerable: true,
        configurable: true
    });
    return TypeDemo;
})();
