var FileSample = (function () {
    function FileSample() {
        this._text = "text";
        this._file = {
            name: "file.txt",
            contents: ""
        };
    }
    Object.defineProperty(FileSample.prototype, "text", {
        /**
        * Gets or sets the text to write/append.
        */
        get: function () {
            return this._text;
        },
        set: function (v) {
            this._text = v;
        },
        enumerable: true,
        configurable: true
    });

    Object.defineProperty(FileSample.prototype, "file", {
        /**
        * Gets the contents of the file.
        */
        get: function () {
            return this._file;
        },
        enumerable: true,
        configurable: true
    });

    /**
    * This function appends a line to the text file.
    */
    FileSample.prototype.appendLine = function () {
        var file = this._file;

        fs.appendLines(file.name, [this._text], function () {
            console.info('append completed');

            fs.readText(file.name, function (t) {
                console.info('read completed');
                file.contents = t;
            });
        });
    };

    /**
    * This function writes some text to the text file.
    */
    FileSample.prototype.writeText = function () {
        var file = this._file;
        fs.writeText(file.name, this._text, function () {
            console.info('write completed');

            fs.readText(file.name, function (t) {
                console.info('read completed');
                file.contents = t;
            });
        });
    };

    /**
    * This function writes some text to the text file.
    */
    FileSample.prototype.appendJson = function () {
        var file = this._file;
        var object = {
            date: new Date(),
            text: this._text
        };

        fs.appendJson(file.name, object, function () {
            console.info('write completed');

            fs.readText(file.name, function (t) {
                console.info('read completed');
                file.contents = t;
            });
        });
    };
    return FileSample;
})();
