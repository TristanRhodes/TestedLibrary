using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Running;

namespace MyBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
        }
    }
}