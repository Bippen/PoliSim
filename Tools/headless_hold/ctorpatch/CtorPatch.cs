// K218 (2026-10-28): Tools/headless_hold's one patch (hold.sh writes its project) to Unity's managed engine module, written to a COPY in the tool's own temp folder (the Editor's
// installed file is only read). UnityEngine.MonoBehaviour's constructor calls an engine-internal check (an ECall), which a plain .NET host refuses; the
// copy's constructor bodies for MonoBehaviour, Behaviour, Component and Object keep their base-constructor call and nothing else, so `new SimulationManager()`
// builds outside the engine. Usage: ctorpatch <UnityEngine.CoreModule.dll> <out.dll>
using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public static class CtorPatch
{
    public static void Main(string[] args)
    {
        ModuleDefinition module = ModuleDefinition.ReadModule(args[0]);
        foreach (string typeName in new[] { "UnityEngine.MonoBehaviour", "UnityEngine.Behaviour", "UnityEngine.Component", "UnityEngine.Object" })
        {
            TypeDefinition type = module.Types.First(t => t.FullName == typeName);
            foreach (MethodDefinition ctor in type.Methods.Where(m => m.IsConstructor && !m.IsStatic))
            {
                Instruction baseCall = ctor.Body.Instructions.FirstOrDefault(i => i.OpCode == OpCodes.Call && ((MethodReference)i.Operand).Name == ".ctor");
                ctor.Body.Instructions.Clear();
                ctor.Body.Variables.Clear();
                ctor.Body.ExceptionHandlers.Clear();
                ILProcessor il = ctor.Body.GetILProcessor();
                if (baseCall != null) { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, (MethodReference)baseCall.Operand); }
                il.Emit(OpCodes.Ret);
            }
        }
        module.Write(args[1]);
        Console.WriteLine("ctorpatch: wrote " + args[1]);
    }
}
