var Survey = (function () {
    function Survey() {
        this._tasks = [
            { question: "Do you agree?", type: "radio", choices: [{ text: "agree", value: null }, { text: "no opinion", value: null }, { text: "disagree", value: null }] },
            { question: "How do you consider yourself?", type: "radio", choices: [{ text: "expert", value: null }, { text: "non-expert", value: null }] },
            { question: "Which ones apply?", type: "checkbox", choices: [{ text: "A", value: null }, { text: "B", value: null }] },
            { question: "What do you think?", type: "text", choices: [{ text: "enter some text here", value: null }] }
        ];
        this._current = {
            begin: new Date(),
            index: 0,
            progress: 0,
            task: this._tasks[0]
        };
    }
    Object.defineProperty(Survey.prototype, "current", {
        get: function () {
            return this._current;
        },
        set: function (v) {
            this._current = v;
        },
        enumerable: true,
        configurable: true
    });

    Survey.prototype.log = function () {
        var current = this._current;
        var answers = [];
        current.task.choices.forEach(function (a) {
            if (a.value != null)
                answers.push({ text: a.text, value: a.value });
        });
        var line = JSON.stringify({
            id: current.index,
            date: new Date(),
            answers: answers
        });

        // append the current answer to the file
        fs.appendLines("survey.txt", [line]);
    };

    Survey.prototype.next = function () {
        // Cache the references
        var current = this._current;
        var tasks = this._tasks;

        // Log the entry
        // this.log();
        // Update the index to the next question
        current.index = current.index + 1;

        // If we don't have any more questions
        if (current.index >= tasks.length) {
            current.task = { question: "Thank you for your help!", type: "radio", choices: [] };
            current.progress = 100;
        } else {
            // Go to the next question
            current.task = tasks[current.index];
            current.progress = 100 * (current.index / tasks.length);
        }
    };

    Survey.prototype.back = function () {
        // Cache the references
        var current = this._current;
        var tasks = this._tasks;

        // Log the entry
        //this.log();
        // Can't go back
        if (current.progress == 0)
            return;

        // Update the index to the next question
        current.index = current.index - 1;

        // Go to the previous question
        current.task = tasks[current.index];
        current.progress = 100 * (current.index / tasks.length);
    };
    return Survey;
})();
