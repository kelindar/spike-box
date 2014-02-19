class AppCache {

    private array: any[] = [];
    private size: number = 0;

    /** 
    * Scans an object and adds it to the cache.
    */
    public scan(target: any): void {
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

                // For each element in the array, check
                for (var i: number = 0; i < target.length; ++i) {
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

            // For each property, recursively scan as well
            for (var propertyName in target) {
                this.scan(target[propertyName]);
            }
        }
    }

    /**
    * Removes empty elements from an array.
    */
    private cleanArray(value: any[]) {
        for (var i = 0; i < value.length; i++) {
            if (typeof (value[i]) == 'undefined' || value[i] == null){
                value.splice(i, 1);
                i--;
            }
        }
        return value;
    }

    /**
    * Adds a new member to the target object.
    */
    public putMember(key: number, propertyName: string, propertyValue: any) {
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
    }

    /**
    * Updates a member of the target object.
    */
    public setMember(key: number, propertyName: string, propertyValue: any) {
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
    }

    /**
    * Deletes a member from the target object.
    */
    public deleteMember(key: number, propertyName: string) {
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

                // First element, just do shift
                case 0: value.shift(); break;

                // Last element, just do pop
                case value.length: value.pop(); break;

                // Default handling, delete and remove empty
                default:
                    delete value[index];
                    this.cleanArray(value);
                    break;
            }

        } else {
            // Set the value of the object
            delete value[propertyName];
        }
    }

    /** 
    * Removes a target item from the cache and returns the removed item.
    */
    public remove(key: number): any {
        if (this.contains(key)) {
            var previous = this.array[key];
            this.size--;
            delete this.array[key];
            return previous;
        }
        else {
            return undefined;
        }
    }

    /** 
    * Checks whether the key is contained in the cache.
    */
    public contains(key: number): boolean{
        return this.array.hasOwnProperty(key.toString());
    }

    /**
    * Gets a value from the cache.
    */
    private getItem(key: number): any {
        return this.contains(key) ? this.array[key] : undefined;
    }

    /**
    * Gets a clone attached to the object.
    */
    public getClone(key: number): any {
        return this.contains(key) ? this.array[key].c : undefined;
    }

    /**
    * Sets a value into the cache.
    */
    private setItem(key: number, value: any) {
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
    }

    /**
    * Clears tne entire cache.
    */
    public clear(): void{
        this.array = [];
        this.size = 0;
    }

    /**
    * Creates a shallow copy of the object.
    */
    private cloneObject(obj: any): any {
        if (null === obj || "object" != typeof obj)
            return obj;
        var copy = obj.constructor();
        for (var attr in obj) {
            if (obj.hasOwnProperty(attr)) copy[attr] = obj[attr];
        }
        return copy;
    }
    
}

