using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spike.Box
{
    /// <summary>
    /// Represents the type of the event that can occur.
    /// </summary>
    public enum AppEventType : byte
    {
        /// <summary>
        /// Represents a custom event, with no particular logic defined.
        /// </summary>
        Custom = 0,

        /// <summary>
        /// Represents a console log or console debug event.
        /// </summary>
        Console = 1,

        /// <summary>
        /// Represents a property change event, when the server notifies the client 
        /// that a property has changed on a particular page.
        /// </summary>
        PropertyChange = 2,

        /// <summary>
        /// Represents a property put event, when a new property was added to an 
        /// object that already have been data-bound.
        /// </summary>
        PropertyPut = 3,

        /// <summary>
        /// Represents a property set event, when a property was changed on an 
        /// object that already have been data-bound.
        /// </summary>
        PropertySet = 4,

        /// <summary>
        /// Represents a property delete event, when a new property was remove from 
        /// an object that already have been data-bound.
        /// </summary>
        PropertyDelete = 5,

    }

}
