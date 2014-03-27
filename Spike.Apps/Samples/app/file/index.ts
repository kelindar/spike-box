class FileSample{

    private _text = "text";
    private _file = {
        name: "file.txt",
        contents: ""
    };
    

    /**
    * Gets or sets the text to write/append.
    */
    public get text() { return this._text; }
    public set text(v) { this._text = v; }

    /**
    * Gets the contents of the file.
    */
    public get file() { return this._file; }

    /**
    * This function appends a line to the text file.
    */
    public appendLine() {
        var file = this._file;

        fs.appendLines(file.name, [this._text], function () {
            console.info('append completed');

            fs.readText(file.name, function (t) {
                console.info('read completed');
                file.contents = t;
            });
        });
    }

    /**
    * This function writes some text to the text file.
    */
    public writeText() {
        var file = this._file;
        fs.writeText(file.name, this._text, function () {
            console.info('write completed');

            fs.readText(file.name, function (t) {
                console.info('read completed');
                file.contents = t;
            });
        });
    }


} 