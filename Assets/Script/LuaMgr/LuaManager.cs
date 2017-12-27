using SLua;
using System;
using UnityEngine;

namespace LuaMgr
{
    
    public class LuaManager 
    {
        private static LuaManager _luaMrg = null;
        private LuaSvr luaSvr;
        private LuaState _luaState;

        private LuaManager()
        {
            LuaHelper.Clear();
            LuaHelper.Init();

            luaSvr = new LuaSvr();
            if(LuaSvr.mainState != null )
            {
                _luaState = LuaSvr.mainState;
                _luaState.loaderDelegate = LuaHelper.DoFile; //自定义加载Lua的文件
            }
        }

        public static LuaManager getInstance()
        {
            if (_luaMrg == null)
            {
                _luaMrg = new LuaManager();

            }
            return _luaMrg;
        }

        public void Start(string main, bool updateSuccess, Action<int> cb)
        {
            luaSvr.init(cb, () => {
                if (cb != null) cb(100);
                SLua.LuaTimer.add(100, (id) => {
                    if (cb != null) cb(101);
                    luaSvr.start(main, updateSuccess, Const.GAME_CURRENT_NAME);
                });
            }, LuaSvrFlag.LSF_BASIC | LuaSvrFlag.LSF_3RDDLL | LuaSvrFlag.LSF_EXTLIB);
        }

        public object CallLuaFunction(string fn, params object[] args)
        {
            LuaFunction func = _luaState.getFunction(fn);
            if (func != null)
            {
                return func.call(args);
            }
            return null;
        }

    }
}
