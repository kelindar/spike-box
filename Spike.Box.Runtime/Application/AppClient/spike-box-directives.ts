angular.module('app').directive('boxApp', function () {
        return function (scope, element, attr) {

            if (attr.boxApp == null || typeof(attr.boxApp) === 'undefined')
                throw new Error("The attribute 'box-app' should contain a value, be bound to {{app}}.");

            if (attr.boxHost == null || typeof (attr.boxHost) === 'undefined')
                throw new Error("The attribute 'box-host' should contain a value, be bound to {{host}}.");

            if (attr.boxView == null || typeof (attr.boxView) === 'undefined')
                throw new Error("The attribute 'box-view' should contain a value, be bound to {{view}}.");

            if (scope.bind == null || typeof (scope.bind) === 'undefined')
                throw new Error("The bind() function was not found. Make sure box-app directive is attached to the same element that contains 'box' controller.");

            var requires = [];
            if (attr.boxRequire != null && typeof (attr.boxRequire) !== 'undefined') {
                try {
                    // Make sure we don't have any quotes
                    var str = attr.boxRequire.toString().trim();
                    str = replaceAll("'", "", str);
                    str = replaceAll("\"", "", str);

                    // Check if the requires contains an array
                    if (str.substring(0, 1) == '[' && str.substring(str.length - 1) == ']') {
                        // Remove the []
                        str = str.substring(1, str.length - 1);

                        // We have an array, loop throug all of its elements
                        var array = str.split(',');
                        for (var i = 0; i < array.length; ++i) {
                            requires.push(array[i].trim());
                        }

                    } else {
                        // No array, just push the require
                        requires.push(str.trim());
                    }
                }
                catch (e) {
                    throw new Error("Unable to parse 'box-require' attribute. Make sure it is a properly formatted array. Error: " + e.message);
                }
            }

            // Do the binding
            scope.bind({
                app: attr.boxApp,
                host: attr.boxHost,
                view: attr.boxView,
                requires: requires
            });

            function replaceAll(find, replace, str) {
                return str.replace(new RegExp(find, 'g'), replace);
            }
        }
});