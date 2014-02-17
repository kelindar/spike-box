var Timer = (function () {
    function Timer() {
        this._counter = {
            count: 0,
            text: ""
        };
        this._timerId = null;
    }
    Object.defineProperty(Timer.prototype, "counter", {
        get: function () {
            return this._counter;
        },
        enumerable: true,
        configurable: true
    });

    Timer.prototype.toggleInterval = function () {
        if (this._timerId == null) {
            // Start the timer
            this._timerId = setInterval(function (obj) {
                obj._counter.count++;
                if (obj._counter.count > 1000)
                    obj._counter.count = 0;
                obj._counter.text = "Counter: " + obj._counter.count;
                console.log("Tick #" + obj._counter.count);
            }, 100, this);
        } else {
            // Stop the timer
            clearInterval(this._timerId);
            this._timerId = null;
        }
    };
    return Timer;
})();
