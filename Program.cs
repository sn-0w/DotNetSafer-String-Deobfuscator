using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNetSafer_StringDeobfuscator
{
    class Program
    {
        static void Main(string[] args)
        {
            ModuleDefMD md = ModuleDefMD.Load(args[0]);

            var stringdecryptormethod2 = md.GlobalType.Methods[1];

            stringdecryptormethod2.ReturnType = md.CorLibTypes.String;
            stringdecryptormethod2.Parameters[0].Type = md.CorLibTypes.String;
            stringdecryptormethod2.Body.Instructions[stringdecryptormethod2.Body.Instructions.Count() - 2].OpCode = OpCodes.Nop;
            stringdecryptormethod2.Body.Instructions[stringdecryptormethod2.Body.Instructions.Count() - 8].OpCode = OpCodes.Nop;
            stringdecryptormethod2.Body.Instructions[stringdecryptormethod2.Body.Instructions.Count() - 10].OpCode = OpCodes.Nop;


            foreach (var type in md.GetTypes())
            {
                if (type == md.GlobalType) continue;
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody || method.Body == null) continue;
                    for (int i = 0; i < method.Body.Instructions.Count(); i++)
                    {
                        var instr = method.Body.Instructions;

                        if (instr[i].OpCode == OpCodes.Br_S && instr[i].Operand == instr[i + 2] && instr[i + 1].OpCode == OpCodes.Unaligned)
                        {
                            instr[i].OpCode = OpCodes.Nop;
                            instr[i + 1].OpCode = OpCodes.Nop;
                        }

                    }
                }
            }
            ModuleWriterOptions opts = new ModuleWriterOptions(md);
            opts.MetadataOptions.Flags = MetadataFlags.KeepOldMaxStack;
            md.Write(args[0].Replace(".exe", "-ignore.exe"), opts);

            // Part 2
            md = ModuleDefMD.Load(args[0].Replace(".exe", "-ignore.exe"));
            Assembly ass = Assembly.LoadFrom(args[0].Replace(".exe", "-ignore.exe"));

            var stringdecryptormethod = md.GlobalType.Methods[1];
            foreach (var type in md.GetTypes())
            {
                if (type == md.GlobalType) continue;
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody || method.Body == null) continue;
                    for (int i = 0; i < method.Body.Instructions.Count(); i++)
                    {
                        var instr = method.Body.Instructions;

                        if (instr[i].OpCode == OpCodes.Br_S && instr[i].Operand == instr[i + 2] && instr[i + 1].OpCode == OpCodes.Unaligned)
                        {
                            instr[i].OpCode = OpCodes.Nop;
                            instr[i + 1].OpCode = OpCodes.Nop;
                        }

                        if (instr[i].OpCode == OpCodes.Call)
                        {
                            IMethod operandmeth = instr[i].Operand as IMethod;
                            if (operandmeth.MDToken == stringdecryptormethod.MDToken)
                            {

                                var local = method.Body.Variables.First();
                                string firsttext ="";
                                int amogus = i;
                                List<object> arguments = new List<object>();
                                while (amogus >= 0)
                                {
                                    amogus--;

                                    if (instr[amogus].OpCode == OpCodes.Ldstr && instr[amogus - 2].OpCode == OpCodes.Dup)
                                    {
                                        arguments.Add(instr[amogus].Operand.ToString());
                                    }

                                    if (instr[amogus].IsLdcI4() && instr[amogus - 2].OpCode == OpCodes.Dup && instr[amogus + 1].Operand.ToString().Contains("Char"))
                                    {
                                        arguments.Add((char)instr[amogus].GetLdcI4Value());
                                    }

                                    if (instr[amogus].IsLdcI4() && instr[amogus - 2].OpCode == OpCodes.Dup && instr[amogus + 1].Operand.ToString().Contains("Int32"))
                                    {
                                        arguments.Add(instr[amogus].GetLdcI4Value());
                                    }

                                    if (instr[amogus].IsLdcI4() && instr[amogus - 2].OpCode == OpCodes.Dup && instr[amogus + 1].Operand.ToString().Contains("Boolean"))
                                    {
                                        arguments.Add((instr[amogus].GetLdcI4Value() == 0) ? false : true );
                                    }

                                    if (instr[amogus].OpCode == OpCodes.Newarr)
                                    {
                                        local = instr[amogus - 2].Operand as Local;
                                        firsttext = instr[amogus - 4].Operand.ToString();
                                        break;
                                    }
                                }
                                int counter = amogus-4;
                                while (counter < i)
                                {
                                    counter++;
                                    instr[counter].OpCode = OpCodes.Nop;
                                }
                                while (instr[counter].OpCode != OpCodes.Ldloc_0 && instr[counter].OpCode != OpCodes.Ldloc_1 && instr[counter].OpCode != OpCodes.Ldloc_2 && instr[counter].OpCode != OpCodes.Ldloc_3 && instr[counter].OpCode != OpCodes.Ldloc_S)
                                {
                                    counter++;
                                }
                                instr[counter].OpCode = OpCodes.Nop;
                                var invokemeth = ass.GetModules().First().ResolveMethod(operandmeth.MDToken.ToInt32());
                                instr[amogus - 4].Operand = invokemeth.Invoke(null,new object[] {firsttext, arguments.ToArray() }).ToString();
                                
                            }
                        }

                        
                    }
                }
            }
            hell.fixcflow(md);
            md.Write(args[0].Replace(".exe", "-deob.exe"), opts);
            Console.WriteLine("done!");
            Console.ReadLine();
        }

        
    }
}