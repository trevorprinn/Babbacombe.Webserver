using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Babbacombe.Webserver {

    /// <summary>
    /// Copies Query Items received from the client into method parameters and properties of
    /// objects in the method parameters.
    /// </summary>
    internal class ObjectBinder {

        public object[] Bind(QueryItems items, MethodInfo method) {
            var pars = method.GetParameters();
            if (!pars.Any()) return null;
            var paramNames = pars.Select(p => p.Name).ToArray();

            var paramObjects = pars.Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : p.ParameterType.GetConstructor(Type.EmptyTypes).Invoke(null)).ToArray();

            if (items == null || !items.Any()) return paramObjects;

            // Go through the value type parameters and put any item values into them directly.
            var itemNames = items.Select(i => i.Name).ToArray();
            for (int i = 0; i < paramObjects.Length; i++) {
                if (!paramObjects[i].GetType().IsValueType) continue;
                if (itemNames.Contains(paramNames[i])) bind(items[paramNames[i]], ref paramObjects[i]);
            }

            // Iterate through the items putting them into matching properties of the parameter objects
            foreach (var item in items) bind(item, paramObjects);

            return paramObjects;
        }

        private void bind(QueryItem item, object[] objects) {
            // Find matching properties in any of the objects
            var props = objects.Select(o => new { Obj = o, Prop = o.GetType().GetProperties().SingleOrDefault(p => p.Name == item.Name && p.CanWrite) })
                .Where(o => o.Prop != null);
            foreach (var op in props) {
                object value;
                if (getTypedValue(op.Prop.PropertyType, item.Value, out value)) {
                    op.Prop.SetValue(op.Obj, value);
                }
            }
        }

        private void bind(string itemValue, ref object o) {
            object value;
            if (getTypedValue(o.GetType(), itemValue, out value)) {
                o = value;
            }
        }

        private bool getTypedValue(Type type, string svalue, out object value) {
            // Initialise the output with the type's default value
            value = type.IsValueType ? Activator.CreateInstance(type) : null;

            var conv = TypeDescriptor.GetConverter(type);
            if (conv.CanConvertFrom(typeof(string))) {
                try {
                    value = conv.ConvertFromString(svalue);
                } catch {
                    return false;
                }
                return true;
            }
            
            return false;
        }
    }
}
