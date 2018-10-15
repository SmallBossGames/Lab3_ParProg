using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace ParProg_Lab3
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            while (true)
            {
                sw.Reset();
                Console.WriteLine("Жду кнопку");
                var check = Console.ReadKey();
                if (check.Key == ConsoleKey.Enter)
                {
                    break;
                }
                sw.Start();
                Quest(8);
                sw.Stop();
                Console.WriteLine($"Я посчиталь c потоками за {sw.ElapsedMilliseconds}");
                sw.Reset();
                sw.Start();
                QuestAsync(8);
                sw.Stop();
                Console.WriteLine($"Я посчиталь c асинхронкой за {sw.ElapsedMilliseconds}");
            }
        }


        static void Quest(int taskCount)
        {
            var tasks = new List<Thread>(taskCount);
            var inputStack = SeedData(100000000);

            for (int i = 0; i < taskCount; i++)
            {
                var writeQueue = new Queue<int>();
                var outputFile = new StreamWriter($"{i}.txt");
                var resetEvent = new ManualResetEventSlim(false);

                var calcThread = new Thread(() => CalcThread(writeQueue, inputStack, resetEvent));
                calcThread.Start();
                var writeThread = new Thread(() => WriteTask(writeQueue, inputStack, resetEvent, outputFile));
                writeThread.Start();

                tasks.Add(calcThread); 
                tasks.Add(writeThread); 
            }

            foreach (var task in tasks) task.Join();  
        }

        static void QuestAsync(int taskCount)
        {
            var tasks = new List<Task>(taskCount);
            var inputStack = SeedData(100000000);

            for (int i = 0; i < taskCount; i++)
            {
                var writeQueue = new Queue<int>();
                var outputFile = new StreamWriter($"{i}.txt");
                var resetEvent = new ManualResetEventSlim(false);

                var calcTask = new Task(() => CalcThread(writeQueue, inputStack, resetEvent));
                calcTask.Start();
                var writeTask = new Task(() => WriteTask(writeQueue, inputStack, resetEvent, outputFile));
                writeTask.Start();

                tasks.Add(calcTask);
                tasks.Add(writeTask);
            }

            foreach (var task in tasks) task.Wait();
        }

        static void CalcThread(
            Queue<int> writeQueue,
            Stack<int> inputStack,
            ManualResetEventSlim resetEvent)
        {
            while (true)
            {
                Monitor.Enter(inputStack);

                if (inputStack.Count == 0)
                {
                    resetEvent.Set();
                    Monitor.Exit(inputStack);
                    return;
                }

                var value = inputStack.Pop();

                Monitor.Exit(inputStack);

                Monitor.Enter(writeQueue);
                writeQueue.Enqueue(value);
                resetEvent.Set();
                Monitor.Exit(writeQueue);
            }
        }

        static void WriteTask(
            Queue<int> writeQueue, 
            Stack<int> inputStack, 
            ManualResetEventSlim resetEvent,
            StreamWriter outputFile)
        {
            while (true)
            {
                Monitor.Enter(writeQueue);

                while (writeQueue.Count == 0)
                {
                    Monitor.Enter(inputStack);
                    var isInputEmpty = inputStack.Count == 0;
                    Monitor.Exit(inputStack);

                    if (isInputEmpty)
                    {
                        outputFile.Close();
                        Monitor.Exit(writeQueue);
                        return;
                    }

                    Monitor.Exit(writeQueue);
                    resetEvent.Wait();
                    Monitor.Enter(writeQueue);
                }

                var value = writeQueue.Dequeue();
                Monitor.Exit(writeQueue);

                if(Prime(value))
                {
                    outputFile.Write($"{value} ");
                }
            }
            
        }

        static bool Prime(int n)
        {
            for (int i = 2; i <= Math.Sqrt(n); i++)
                if (n % i == 0)
                    return false;
            return true;
        }

        static Stack<int> SeedData(int count)
        {
            Stack<int> enter = new Stack<int>();
            var rand = new Random();
            for (var i = 0; i < count; i++)
            {
                enter.Push(rand.Next());
            }
            return enter;
        }

    }
}
