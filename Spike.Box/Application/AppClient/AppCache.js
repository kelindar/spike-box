var AppCache = (function () {
    function AppCache() {
        this.array = [];
        this.size = 0;
    }
    /**
    * Scans an object and adds it to the cache.
    */
    AppCache.prototype.scan = function (target) {
        // Make sure we have a target
        if (typeof (target) == 'undefined' || target == null)
            return;

        // If the value is an array, convert it
        if (Object.prototype.toString.call(target) === '[object Array]') {
            if (target.length > 0) {
                // Get the last element and assign the id to the array
                var arrayId = target['$i'] = parseInt(target.pop());

                // If we have it already in the cache, ignore
                if (this.contains(arrayId))
                    return;

                // Set to the cache
                this.setItem(arrayId, target);

                for (var i = 0; i < target.length; ++i) {
                    this.scan(target[i]);
                }
            }
        } else {
            // Make sure we have a usable id
            if (!target.hasOwnProperty('$i'))
                return;

            // Get the identifier
            var id = target['$i'];

            // If we have it already in the cache, ignore
            if (this.contains(id))
                return;

            // Set to the cache
            this.setItem(id, target);

            for (var propertyName in target) {
                this.scan(target[propertyName]);
            }
        }
    };

    /**
    * Removes empty elements from an array.
    */
    AppCache.prototype.cleanArray = function (value) {
        for (var i = 0; i < value.length; i++) {
            if (typeof (value[i]) == 'undefined' || value[i] == null) {
                value.splice(i, 1);
                i--;
            }
        }
        return value;
    };

    /**
    * Adds a new member to the target object.
    */
    AppCache.prototype.putMember = function (key, propertyName, propertyValue) {
        // Get the item by the key and make sure it exists
        var target = this.getItem(key);

        // Get the value and the clone
        var value = target.v;
        var clone = target.c;
        if (typeof (value) == 'undefined' || value == null)
            return;

        // Two cases, one for the array and one for the object
        if (Object.prototype.toString.call(value) === '[object Array]') {
            // Set the value of the array
            value[parseInt(propertyName)] = propertyValue;
            this.cleanArray(value);
        } else {
            // If it already has the property, ignore
            if (value.hasOwnProperty(propertyName))
                return;

            // Set the value of the object
            value[propertyName] = propertyValue;
        }

        // Scan the new value
        this.scan(propertyValue);
    };

    /**
    * Updates a member of the target object.
    */
    AppCache.prototype.setMember = function (key, propertyName, propertyValue) {
        // Get the item by the key and make sure it exists
        var target = this.getItem(key);

        // Get the value and the clone
        var value = target.v;
        var clone = target.c;
        if (typeof (value) == 'undefined' || value == null)
            return;

        // Two cases, one for the array and one for the object
        if (Object.prototype.toString.call(value) === '[object Array]') {
            // Set the value of the array
            value[parseInt(propertyName)] = propertyValue;
            this.cleanArray(value);
        } else {
            // If it doesn't have the property, ignore
            if (!value.hasOwnProperty(propertyName))
                return;

            // Set the value of the object
            value[propertyName] = propertyValue;
        }

        // Scan the new value
        this.scan(propertyValue);
    };

    /**
    * Deletes a member from the target object.
    */
    AppCache.prototype.deleteMember = function (key, propertyName) {
        // Get the item by the key and make sure it exists
        var target = this.getItem(key);

        // Get the value and the clone
        var value = target.v;
        var clone = target.c;
        if (typeof (value) == 'undefined' || value == null)
            return;

        // If it doesn't have the property, ignore
        if (!value.hasOwnProperty(propertyName))
            return;

        // If it's an array, overwrite the index
        // Two cases, one for the array and one for the object
        if (Object.prototype.toString.call(value) === '[object Array]') {
            // Set the value of the array
            var index = parseInt(propertyName);

            switch (index) {
                case 0:
                    value.shift();
                    break;

                case value.length:
                    value.pop();
                    break;

                default:
                    delete value[index];
                    this.cleanArray(value);
                    break;
            }
        } else {
            // Set the value of the object
            delete value[propertyName];
        }
    };

    /**
    * Removes a target item from the cache and returns the removed item.
    */
    AppCache.prototype.remove = function (key) {
        if (this.contains(key)) {
            var previous = this.array[key];
            this.size--;
            delete this.array[key];
            return previous;
        } else {
            return undefined;
        }
    };

    /**
    * Checks whether the key is contained in the cache.
    */
    AppCache.prototype.contains = function (key) {
        return this.array.hasOwnProperty(key.toString());
    };

    /**
    * Gets a value from the cache.
    */
    AppCache.prototype.getItem = function (key) {
        return this.contains(key) ? this.array[key] : undefined;
    };

    /**
    * Gets a clone attached to the object.
    */
    AppCache.prototype.getClone = function (key) {
        return this.contains(key) ? this.array[key].c : undefined;
    };

    /**
    * Sets a value into the cache.
    */
    AppCache.prototype.setItem = function (key, value) {
        if (this.contains(key)) {
            // Set the object and replace the old one
            this.array[key] = {
                v: value,
                c: this.cloneObject(value)
            };
        } else {
            // Put a new object
            this.size++;
            this.array[key] = {
                v: value,
                c: this.cloneObject(value)
            };
        }
    };

    /**
    * Clears tne entire cache.
    */
    AppCache.prototype.clear = function () {
        this.array = [];
        this.size = 0;
    };

    /**
    * Creates a shallow copy of the object.
    */
    AppCache.prototype.cloneObject = function (obj) {
        if (null === obj || "object" != typeof obj)
            return obj;
        var copy = obj.constructor();
        for (var attr in obj) {
            if (obj.hasOwnProperty(attr))
                copy[attr] = obj[attr];
        }
        return copy;
    };
    return AppCache;
})();
