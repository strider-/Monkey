using System.Collections.Generic;

namespace Monkey.Evaluation
{
    public class Environment
    {
        private Dictionary<string, IObject> _store = new Dictionary<string, IObject>();
        private readonly Environment _outer;

        public Environment() : this(null) { }

        public Environment(Environment outer)
        {
            _outer = outer;
        }

        public bool Get(string name, out IObject obj)
        {
            var ok = _store.TryGetValue(name, out obj);
            if (!ok && _outer != null)
            {
                ok = _outer.Get(name, out obj);
            }
            return ok;
        }

        public IObject Set(string name, IObject value)
        {
            _store[name] = value;
            return value;
        }
    }
}
