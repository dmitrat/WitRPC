using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace OutWit.Common.Reflection
{
    public static class EventUtils
    {
        public static IEnumerable<EventInfo> GetAllEvents(this Type type)
        {
            var events = new HashSet<EventInfo>(type.GetEvents());

            foreach (var baseInterface in type.GetInterfaces())
                events.UnionWith(baseInterface.GetAllEvents());

            if (type.BaseType != null)
                events.UnionWith(type.BaseType.GetAllEvents());

            return events;
        }

        public static Delegate CreateUniversalHandler<TSender>(this EventInfo me, TSender sender, UniversalEventHandler<TSender> handler)
            where TSender: class
        {
            if (!handler.Method.IsStatic)
                throw new Exception("Universal event handler delegate must be static");

            Type handlerType = me.EventHandlerType!;
            MethodInfo invokeMethod = handlerType.GetMethod("Invoke")!;
            ParameterInfo[] parameters = invokeMethod.GetParameters();
            IList<Type> parameterTypes = parameters.Select(info => info.ParameterType).ToList();
            parameterTypes.Insert(0, typeof(TSender));

            var dynamicMethod = new DynamicMethod(
                $"DynamicHandler_{me.Name}",
                invokeMethod.ReturnType,
                parameterTypes.ToArray(),
                typeof(TSender)
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            LocalBuilder argsArray = il.DeclareLocal(typeof(object[]));
            il.Emit(OpCodes.Ldc_I4, parameters.Length); 
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, argsArray);

            for (int i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldloc, argsArray);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, i + 1);

                if (parameters[i].ParameterType.IsValueType)
                    il.Emit(OpCodes.Box, parameters[i].ParameterType);

                il.Emit(OpCodes.Stelem_Ref);
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, me.Name);
            il.Emit(OpCodes.Ldloc, argsArray);

            il.Emit(OpCodes.Call, handler.Method);

            il.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(handlerType, sender);
        }
    }

    public delegate void UniversalEventHandler<in TSender>(TSender sender, string eventName, object[] parameters) 
        where TSender : class;

}
