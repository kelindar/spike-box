var BufferSample = (function () {
    function BufferSample() {
        this._text = "encoded text";
        this._info = {
            length: 0,
            base64: "",
            hex: "",
            json: "",
            slice: "",
            slice2: ""
        };
    }
    Object.defineProperty(BufferSample.prototype, "text", {
        get: function () {
            return this._text;
        },
        set: function (v) {
            // Set the text property
            this._text = v;

            // A new buffer that contains the utf-8 encoded string
            var buffer = new Buffer(v, 'utf8');

            // Convert and store
            this._info.base64 = buffer.toString('base64');
            this._info.hex = buffer.toString('hex');
            this._info.json = buffer.toJSON();
            this._info.length = buffer.length;
            this._info.slice = buffer.length > 3 ? buffer.slice(1, 3).toString('utf8') : "";
            this._info.slice2 = buffer.length > 3 ? buffer.slice(1, 3).slice(1, 2).toString('utf8') : "";
        },
        enumerable: true,
        configurable: true
    });

    Object.defineProperty(BufferSample.prototype, "info", {
        get: function () {
            return this._info;
        },
        enumerable: true,
        configurable: true
    });

    BufferSample.prototype.write = function () {
        var buf = new Buffer(8);

        buf[0] = 0x55;
        buf[1] = 0x55;
        buf[2] = 0x55;
        buf[3] = 0x55;
        buf[4] = 0x55;
        buf[5] = 0x55;
        buf[6] = 0xd5;
        buf[7] = 0x3f;

        console.log(buf.readDoubleLE(0));

        buf.writeDoubleBE(0.005, 0);
        console.log(buf);
        console.log(buf.readDoubleBE(0));
    };
    return BufferSample;
})();
