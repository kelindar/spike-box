class Timer{

    private _counter = {
         value: 0
        };
    private _timerId = null;

    public get counter() { return this._counter; }

    public toggleInterval() {
        if (this._timerId != null) {
            // Stop the timer
            clearInterval(this._timerId);
            this._timerId = null;
            return;
        }

        // Start the timer
        this._timerId = setInterval(() => {
            this._counter.value++;
            if (this._counter.value > 1000)
                this._counter.value = 0;
        }, 100);
    }
} 