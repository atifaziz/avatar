﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Diagnostics;
using TypeNameFormatter;

namespace Stunts
{
    [DebuggerTypeProxy(typeof(DebugView))]
    [DebuggerDisplay("Count = {Count}")]
    class ArgumentCollection : IArgumentCollection
    {
        readonly List<ParameterInfo> infos;
        readonly List<object?> values;

        public ArgumentCollection(object?[] values, ParameterInfo[] infos)
            : this((IEnumerable<object>)values, (IEnumerable<ParameterInfo>)infos) { }

        public ArgumentCollection(IEnumerable<object?> values, IEnumerable<ParameterInfo> infos)
        {
            this.infos = infos.ToList();
            this.values = values.ToList();

            if (this.infos.Count != this.values.Count)
                throw new ArgumentException("Number of arguments must match number of parameters.");
        }

        public object? this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }

        public object? this[string name]
        {
            get => values[ValidIndexOf(name)];
            set => values[ValidIndexOf(name)] = value;
        }

        public int Count => infos.Count;

        public bool Contains(string name) => IndexOf(name) != -1;

        public IEnumerator<object?> GetEnumerator() => values.GetEnumerator();

        public ParameterInfo GetInfo(string name) => infos[ValidIndexOf(name)];

        public ParameterInfo GetInfo(int index) => infos[index];

        public string NameOf(int index) => infos[index].Name;

        public int IndexOf(string name)
        {
            for (var i = 0; i < infos.Count; ++i)
            {
                if (infos[i].Name == name)
                    return i;
            }

            return -1;
        }

        public override string ToString() => string
            .Join(", ", infos
                .Select((p, i) =>
                    (p.IsOut ? p.ParameterType.GetFormattedName().Replace("ref ", "out ") : p.ParameterType.GetFormattedName()) +
                    " " + p.Name +
                    (p.IsOut ? "" :
                        (" = " +
                            ((IsString(p.ParameterType) && values[i] != null) ? "\"" + values[i] + "\"" :
                                // render boolean as lowercase to match C#
                                (values[i] is bool b) ? b.ToString().ToLowerInvariant() : (values[i] ?? "null"))
                        )
                    )
                )
            );

        int ValidIndexOf(string name)
        {
            var index = IndexOf(name);
            if (index == -1)
                throw new KeyNotFoundException(name);

            return index;
        }

        static bool IsString(Type type) => type == typeof(string) ||
            (type.IsByRef && type.HasElementType && type.GetElementType() == typeof(string));

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [DebuggerNonUserCode]
        class DebugView
        {
            readonly ArgumentCollection arguments;
            public DebugView(ArgumentCollection arguments) => this.arguments = arguments;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<ParameterInfo, object?>[] Items => arguments.infos
                .Select((info, index) => new KeyValuePair<ParameterInfo, object?>(info, arguments.values[index]))
                .ToArray();
        }
    }
}
