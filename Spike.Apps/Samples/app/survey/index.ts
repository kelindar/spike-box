class Survey{

    private _tasks = [
        { question: "Do you agree?", type: "radio", choices: [{ text: "agree", value: null }, { text: "no opinion", value: null }, { text: "disagree", value: null}] },
        { question: "How do you consider yourself?", type: "radio", choices: [{ text: "expert", value: null }, { text: "non-expert", value: null }] },
        { question: "Which ones apply?", type: "checkbox", choices: [{ text: "A", value: null }, { text: "B", value: null }] },
        { question: "What do you think?", type: "text", choices: [{ text: "enter some text here", value: null }] },
    ];

    public get current() { return this._current; }
    public set current(v) { this._current = v; }
    private _current = {
        begin: new Date(),
        index: 0,
        progress: 0,
        task: this._tasks[0]
    }

    /**
    * Appends a line to the survey log.
    */
    private log() {
        // Select the answers
        var current = this._current;
        var answers = [];
        current.task.choices.forEach(function(a){
            if (a.value != null)
                answers.push({ text:a.text, value:a.value });
        });

        // If we have no answers, don't log
        if (answers.length == 0)
            return;

        // Convert to a string
        var line = JSON.stringify({
            id: current.index,
            date: new Date(),
            answers: answers
        });

        // append the current answer to the file
        fs.appendLines("survey.txt", [line]);
    }

    /**
    * Goes to the next question.
    */
    public next() {
        // Cache the references
        var current = this._current;
        var tasks = this._tasks;

        // Log the entry
        this.log();

        // Update the index to the next question
        current.index = current.index + 1;

        // If we don't have any more questions
        if (current.index >= tasks.length) {
            current.task = { question: "Thank you for your help!", type: "radio", choices: [] }
            current.progress = 100;
        } else {
            // Go to the next question
            current.task = tasks[current.index];
            current.progress = 100 * (current.index / tasks.length);
        }

    }

    /**
    * Goes to the previous question.
    */
    public back() {
        // Cache the references
        var current = this._current;
        var tasks = this._tasks;

        // Log the entry
        this.log();

        // Can't go back
        if (current.progress == 0)
            return;

        // Update the index to the next question
        current.index = current.index - 1;

        // Go to the previous question
        current.task = tasks[current.index];
        current.progress = 100 * (current.index / tasks.length);
        
    }
} 