var app = angular.module('app', []);

// Returns a angular module, searching for its name, if it's a string
function getModule(name) {
    if (typeof name === "string") {
        return angular.module(name);
    }
    return name;
};

var moduleExtender = function (sourceModule) {
    var modules = Array.prototype.slice.call(arguments);

    // Take sourceModule out of the array
    modules.shift();

    // Parse the source module
    sourceModule = getModule(sourceModule);
    if (!sourceModule._appBoxDecorated) {
        throw new Error("Can't extend a module which hasn't been decorated.");
    }

    // Merge all modules into the source module
    modules.forEach(function (module) {
        module = getModule(module);
        module._invokeQueue.reverse().forEach(function (call) {
            // call is in format [ provider, function, args ]
            var provider = sourceModule._lazyProviders[call[0]];

            // Same as for example $controllerProvider.register("Ctrl", function() { ... })
            provider && provider[call[1]].apply(provider, call[2]);
        });
    });
};

/**
* Allows lazy extension, loading modules asychronously.
*/
app.extend = moduleExtender.bind(null, app);

/**
* Configures the application.
*/
app.config([
    '$controllerProvider', '$compileProvider', '$filterProvider', '$provide',
    function ($controllerProvider, $compileProvider, $filterProvider, $provide) {
        // Lazy providers
        app._lazyProviders = {
            $compileProvider: $compileProvider,
            $controllerProvider: $controllerProvider,
            $filterProvider: $filterProvider,
            $provide: $provide
        };

        // Lazy functions
        app.lazy = {
            controller: $controllerProvider.register,
            directive: $compileProvider.directive,
            filter: $filterProvider.register,
            factory: $provide.factory,
            service: $provide.service,
            extend: app.extend
            //animate: $animateProvider.register
        };

        // Register routes with the $routeProvider

        // Set the decorated field
        app._appBoxDecorated = true;
}]);

if (!Object.keys) {
    Object.keys = function (obj) {
        var keys = [],
            k;
        for (k in obj) {
            if (Object.prototype.hasOwnProperty.call(obj, k)) {
                keys.push(k);
            }
        }
        return keys;
    };
}

app.factory('$server', ['$q', '$rootScope', function ($q, $rootScope) {
    // We return this object to anything injecting our service
    var service = {};

    // Keep all pending requests here until they get responses
    var callbacks = {};

    // Create a unique callback ID to map requests to responses
    var currentCallbackId = 0;

    // Create a unique control identifier for that page
    var currentInstanceId = 0;

    // Create the actual channel used for mapping
    var server = null;

    /**
    * Creates a new callback identifier for a request.
    */
    function getCallbackId() {
        currentCallbackId += 1;
        if (currentCallbackId > 10000) {
            currentCallbackId = 0;
        }
        return currentCallbackId;
    }

    /**
    * Creates a new instance identifier for a handshake request.
    */
    function getInstanceId() {
        return ++currentInstanceId;
    }


    /**
    * Gets the session cookie value.
    */
    function getSessionId() {
        var name = "spike-session";
        var value = "; " + document.cookie;
        var parts = value.split("; " + name + "=");
        if (parts.length == 2) return parts.pop().split(";").shift();
    }

    /**
    * Gets the application id.
    */
    function getAppId() {
        return service.currentApp;
    }

    /**
    * Prints the log in the console.
    */
    function log(method, args) {
        console[method](service.deserialize(args));
    }

    /**
    * Create the application cache, used for property value mapping.
    */
    service.cache = new AppCache();

    /**
    * An object that contains receiving flags.
    */
    service.receiving = {};

    /**
    * Serializes the arguments and pushes them in an appropriate array.
    */
    service.makeArgs = function(args){
        var array = [];
        for (var i = 0; i < args.length; i++) {
            array.push(service.serialize(args[i]));
        }

        if (array.length == 0)
            array = null;

        return array;
    }

    /**
    * Parses the data appropriately and returns a deserialized object.
    */
    service.deserialize = function(data) {
        try {
            // Try to convert from JSON
            data = JSON.parse(data);

            // Validate the arrays
            service.cache.validate(data);
        } catch (error) {
            // Not a JSON, continue
        }
        return data;
    }

    /**
    * Serializes the data in a JSON array of arguments.
    */
    service.serialize = function (data) {

        // If undefined or null, return null
        if (typeof (data) == 'undefined' || data == null)
            return null;

        return JSON.stringify(data);
    }

    /**
    * Sends a handshake request to the server.
    */
    service.handshake = function (callback, view, element) {
        var appId = getAppId();
        var sessionId = getSessionId();
        var callbackId = getCallbackId();

        // Prepare the callback and the promise
        var defer = $q.defer();
        callbacks[callbackId] = {
            time: new Date(),
            cb: defer
        };

        // Prepare the handshake request
        var request = {
            app: appId,
            session: sessionId,
            view: view
        };

        // If we have specified an element, we must add the instance id
        // to the request
        if (typeof (element) !== 'undefined' && element != null) {
            request.element = element;
            request.instance = getInstanceId();
        }

        // Send a handshake request with the request serialized in 
        // JSON format.
        server.handshake(JSON.stringify(request), callbackId);

        // Register the callback promise and return it
        defer.promise.then(callback);
        return defer.promise;
    }

    /**
    * Represents a constructor that sends the appropriate handshake for a view.
    */
    service.view = function (name, callback) {
        service.handshake(callback, name);
    }

    /**
    * Represents a constructor that sends the appropriate handshake for an element.
    */
    service.element = function (name, view, callback) {
        service.handshake(callback, view, name);
    }


    /**
    * Sends a request to the server
    */
    service.query = function (target, method, args, callback) {
        var sessionId = getSessionId();
        var callbackId = getCallbackId();

        if (typeof (target) === 'undefined' || target == null)
            return;

        // Prepare the callback and the promise
        var defer = $q.defer();
        callbacks[callbackId] = {
            time: new Date(),
            cb: defer
        };

        // Publish to the server and expect a promise
        server.query(sessionId, callbackId, target, method, args);
        defer.promise.then(callback);
        return defer.promise;
    }


    /**
    * Occurs when an angularjs watch fires and a property needs to be updated
    */
    service.onPropertySet = function (newValue, oldValue) {


        // Right now, we only support 'set' type
        service.onPropertyChange(4, newValue.name, newValue.target);
    }

    /**
    * Occurs when an angularjs watch fires and a property needs to be updated
    */
    service.onPropertyChange = function (changeType, propertyName, target) {
        // If it's not an object, ignore
        if (!target.hasOwnProperty('$i'))
            return;

        // If we're currently receiving the value, do not do anything
        var id = target['$i'];
        if (service.receiving[id])
            return;

        // Get the app id and the session
        var sessionId  = getSessionId();
        var serialized = service.serialize(target[propertyName]);

        // Publish to the server and don't wait for any response
        server.notify(sessionId, changeType, id, propertyName, serialized);
    }

    /**
    * Performs the connection to a remote endpoint and binds the callbacks.
    */
    service.connect = function (endpoint, callback) {

        // Create a new channel
        server = new ServerChannel(endpoint);

        // Bind the callback if provided
        if (callback != null && typeof (callback) !== 'undefined') {
            server.onConnect(callback);
        }

        /**
        * Occurs when we have received a handhsake response from the server.
        */
        server.handshakeInform = function (packet) {
            // Get the members of the packet
            var cid = packet.callback;
            var oid = packet.target;

            // console.log(packet);

            // Resolve the appropriate promise
            if (callbacks.hasOwnProperty(cid)) {
                $rootScope.$apply(callbacks[cid].cb.resolve(oid));
                delete callbacks[cid];
            }
        };

        // Occurs when we get a notification from the server
        server.queryInform = function (packet) {
            // Get the members of the packet
            var cid = packet.callback;
            var data = service.deserialize(packet.result);

            //console.log(packet);

            // Resolve the appropriate promise
            if (callbacks.hasOwnProperty(cid)) {

                // Make sure we have the object in the cache
                service.cache.scan(data);

                // Execute the apply on the scope
                $rootScope.$apply(callbacks[cid].cb.resolve(data));
                delete callbacks[cid];
            }

        };

        server.eventInform = function (packet) {
            var type = packet.type;
            var target = packet.target;
            var name = packet.name;

            // The array to hold all nested receive ids
            var nested = [];

            // We're receiving the target
            service.receiving[target] = true;

            //console.log(packet);

            try {
                // Deserialize the value
                var value = service.deserialize(packet.value);

                // Get all the identifiers we've received and mark them as "receiving".
                nested = service.cache.getIds(value);
                for (var i = 0; i < nested.length; ++i) {
                    service.receiving[nested[i]] = true;
                }

                // If it's a property set on a page, update the cache
                if (type == 4) {
                    service.cache.scan(value);
                }

                switch (type) {
                    case 0: $rootScope.$broadcast('e:custom', packet); break;
                    case 1: log(name, value); break;
                    case 2: $rootScope.$broadcast('e:property', packet); break;
                    case 3: service.cache.putMember(target, name, value); break;
                    case 4: service.cache.setMember(target, name, value); break;
                    case 5: service.cache.deleteMember(target, name, value); break;
                }

                $rootScope.$apply();
            }
            finally {
                // We're done with reception handling of the target
                service.receiving[target] = false;

                // We're done with reception handling of all the nested objects
                for (var i = 0; i < nested.length; ++i) {
                    service.receiving[nested[i]] = false;
                }
            }
        };
    }



    return service;
}]);


app.controller('box', ['$scope', '$server', function ($scope, $server) {

    $scope.bind = function (config) {
        // Set the application identifier and a view url
        $server.currentApp = config.app;
        $scope.url = config.view;

        // Connect to the host
        $server.connect(config.host);

        // If we have a require, inject the dependancies
        if (config.requires != null && typeof (config.requires) !== 'undefined' && config.requires.length > 0) {
            for (var i = 0; i < config.requires.length; ++i) {
                // Get the dependancy
                var dependancy = config.requires[0];

                // Add to the requires list
                app.lazy.extend(dependancy);
            }
        }
    };

}]);


/**
* An extention to angularjs that watches an object.
*/
angular.watchObject = function ($server, $parse, obj, listener) {
    var self = this;

    var objGetter = $parse(obj);
    var unchanged = false;
    var oldSimple = null;

    // The property values sent to the listener
    var newPropertyValue = { target: null, name: null, value: null };
    var oldPropertyValue = { target: null, name: null, value: null };

    function scanSimple(newValue) {
        // It's not an object, but we have a setter, therefore we must
        // serialize the value and propagate to the server.

        // Property set:
        if (newValue != oldSimple) {

            // set the new value arguments
            newPropertyValue.target = self;
            newPropertyValue.name = obj;
            newPropertyValue.value = newValue;

            // set the old value arguments
            oldPropertyValue.target = self;
            oldPropertyValue.name = obj;
            oldPropertyValue.value = oldSimple;

            // Since there was a change, set the clone
            oldSimple = newValue;

            return true;
        }

        return false;
    }

    function scanChange(newValue) {
        // Make sure we have a valid object
        if (typeof (newValue) === 'undefined' || newValue == null)
            return;

        // Make sure we have an id
        if (!newValue.hasOwnProperty('$i')) 
            return false;

        var id = newValue.$i;
        var oldValue = $server.cache.getClone(id);
        if (typeof (oldValue) === 'undefined' || oldValue == null)
            return;

        if (Object.prototype.toString.call(newValue) === '[object Array]') {
            // Check every item
            var item;
            for (var i = 0; i < newValue.length; i++) {
                item = newValue[i];
                if (typeof (item) === 'undefined' || item == null)
                    continue;

                // Scan item for changes
                if (scanChange(item))
                    return true;
            }

        } else {
            // Check every sub property
            for (var propertyName in newValue) {

                // Property put:
                if (!oldValue.hasOwnProperty(propertyName))
                    continue;

                // Property set:
                if (newValue[propertyName] != oldValue[propertyName]) {

                    // set the new value arguments
                    newPropertyValue.target = newValue;
                    newPropertyValue.name = propertyName;
                    newPropertyValue.value = newValue[propertyName];

                    // set the old value arguments
                    oldPropertyValue.target = newValue;
                    oldPropertyValue.name = propertyName;
                    oldPropertyValue.value = oldValue[propertyName];

                    // Since there was a change, set the clone
                    oldValue[propertyName] = newValue[propertyName];

                    return true;
                } else {

                    // Scan child for changes
                    if (scanChange(newValue[propertyName]))
                        return true;
                }
            }
        }
        return false;
    }


    function $watchObjectWatch(scope) {

        // Execute the getter and get the new value
        var newValue = objGetter(self);

        // Scan for changes
        var changed = (typeof (newValue) !== 'undefined' && newValue != null && newValue.hasOwnProperty('$i'))
            ? scanChange(newValue)
            : scanSimple(newValue);

        // Toggle the change
        if (changed)
            unchanged = !unchanged;
        return unchanged;
    }

    function $watchObjectAction() {

        if (oldPropertyValue.target != null)
            listener(newPropertyValue, oldPropertyValue, self);
    }

    return this.$watch($watchObjectWatch, $watchObjectAction);
};