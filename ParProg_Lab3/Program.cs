using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace ParProg_Lab3
{
    class Program
    {
        static void Main(string[] args)
        {
            Quest(6);
            Console.WriteLine("Я посчиталь");
            Console.ReadLine();
            QuestAsync(6);
            Console.WriteLine("Я посчиталь асинхронно");
            Console.ReadLine();
        }

        static void Quest(int taskCount)
        {
            var tasks = new List<Thread>(taskCount);
            var inputStack = SeedData(10000000);

            for (int i = 0; i < taskCount; i++)
            {
                var writeQueue = new Queue<int>();
                var outputFile = new StreamWriter($"{i}.txt");
                var resetEvent = new ManualResetEventSlim(false);

                var calcThread = new Thread(() => CalcThread(writeQueue, inputStack, resetEvent));

                var writeThread = new Thread(() => WriteTask(writeQueue, inputStack, resetEvent, outputFile));

                tasks.Add(calcThread); calcThread.Start();
                tasks.Add(writeThread); writeThread.Start();
            }

            foreach (var task in tasks) task.Join();
        }

        static void QuestAsync(int taskCount)
        {
            var tasks = new List<Task>(taskCount);
            var inputStack = SeedData(3, 10000000);

            for (int i = 0; i < taskCount; i++)
            {
                var writeQueue = new Queue<int>();
                var outputFile = new StreamWriter($"{i}.txt");
                var resetEvent = new ManualResetEventSlim(false);

                tasks.Add(new Task(() => CalcThread(writeQueue, inputStack, resetEvent)));
                tasks.Add(new Task(() => WriteTask(writeQueue, inputStack, resetEvent, outputFile)));
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

                if (Prime(value))
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
            Console.WriteLine("Моя сгенерировал");
            Console.ReadLine();
            return enter;
        }

        static Stack<int> SeedData(int from, int n)
        {
            Stack<int> enter = new Stack<int>();

            for (var i = 3; i < n; i++) enter.Push(i);

            Console.WriteLine("Моя сгенерировал");
            Console.ReadLine();
            return enter;
        }
    }
}