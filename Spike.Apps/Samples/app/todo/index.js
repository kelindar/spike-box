var Todo = (function () {
    function Todo() {
        this._todos = [
            { text: 'implement a handshake protocol between the client and the server', done: true },
            { text: 'populate properties after handshake', done: true },
            { text: 'figure out how to raise events from F#', done: true },
            { text: 'convert console methods to native implementation', done: true },
            { text: 'convert defineProperty method to native implementation', done: true },
            { text: 'have a unique id for each ScriptObject that can be used to track changes', done: true },
            { text: 'handle 3 possible changes to objects: property add, property change and property delete', done: true },
            { text: 'minify client-side javascript in the angular template', done: true },
            { text: 'on the client, have a hashtable that contains $i and the reference for updates', done: true },
            { text: 'transmit & handle Object changed events', done: true },
            { text: 'transmit & handle Array changed events', done: true },
            { text: 'only assign $i to the object that are observed', done: true },
            { text: 'when converting an object to JSON, skip fields prefixed with _ (private)', done: false },
            { text: 'implement Array methods according to ECMAScript5 specs', done: true },
            { text: 'correctly handle scope disposal', done: false },
            { text: 'if a property has public setter, transmit updates back to server, recursively', done: true },
            { text: 'computed properties should be transmitted automatically', done: true },
            { text: 'track all updates within all exposed properties, recursively', done: true },
            { text: 'implement angularjs components', done: true },
            { text: 'implement components isolate scope', done: true },
            { text: 'implement angularjs filters', done: false },
            { text: 'implement page composability, where a view can be included into another view', done: true },
            { text: 'implement a shared session api', done: false },
            { text: 'support multi-app host', done: false }
        ];
        this._todoText = "";
    }
    Object.defineProperty(Todo.prototype, "todoText", {
        get: function () {
            return this._todoText;
        },
        enumerable: true,
        configurable: true
    });

    Object.defineProperty(Todo.prototype, "todos", {
        get: function () {
            return this._todos;
        },
        set: function (v) {
            this._todos = v;
        },
        enumerable: true,
        configurable: true
    });

    Object.defineProperty(Todo.prototype, "remaining", {
        get: function () {
            var count = 0;
            this._todos.forEach(function (todo) {
                count += todo.done ? 0 : 1;
            });
            return count;
        },
        enumerable: true,
        configurable: true
    });

    Todo.prototype.archive = function () {
        this._todos.removeAll(function (todo) {
            return todo.done;
        });
    };

    Todo.prototype.addTodo = function (t) {
        this._todos.push({ text: t, done: false });
        this.todoText('');
    };
    return Todo;
})();
