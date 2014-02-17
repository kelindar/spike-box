class Timer{

    private _counter = {
        count: 0,
        text: ""
    };
    
    private _timerId = null;

    public get counter() { return this._counter; }

    public toggleInterval() {
        if (this._timerId == null){
            // Start the timer
            this._timerId = setInterval(function (obj) {
                obj._counter.count++;
                if (obj._counter.count > 1000)
                    obj._counter.count = 0;
                obj._counter.text = "Counter: " + obj._counter.count;
				console.log("Tick #" + obj._counter.count);
            }, 100, this);
        }else{
            // Stop the timer
            clearInterval(this._timerId);
            this._timerId = null;
        }
    }
} 