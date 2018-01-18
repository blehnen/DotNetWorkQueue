using System.Collections.Generic;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Ensures that every script is complied
    /// </summary>
    internal class LuaScripts
    {
        private readonly IEnumerable<BaseLua> _scripts;
        /// <summary>
        /// Initializes a new instance of the <see cref="LuaScripts"/> class.
        /// </summary>
        /// <param name="scripts">The scripts.</param>
        public LuaScripts(IEnumerable<BaseLua> scripts)
        {
            _scripts = scripts;
        }
        /// <summary>
        /// Setup each LUA script
        /// </summary>
        public void Setup()
        {
            foreach (var script in _scripts)
            {
                script.LoadScript();
            }
        }
    }
}
