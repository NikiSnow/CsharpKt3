using System;
using System.Collections.Generic;

namespace MemoryOptimizationDemo
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== Memory demo ===\n");

            RunBad();
            Console.WriteLine();
            RunOptimized();

            Console.WriteLine("\nDone. Press Enter.");
            Console.ReadLine();
        }

        static void RunBad()
        {
            Console.WriteLine("[BAD] CustomerProcessorBad");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before = GC.GetTotalMemory(true);

            var bad = new CustomerProcessorBad();
            bad.Processed += OnProcessed;
            bad.LoadCustomers(100_000);
            bad.ProcessCustomers();
            // объект bad остаётся жить до конца метода, ссылки на коллекцию и событие не очищены

            long after = GC.GetTotalMemory(true);
            Console.WriteLine($"[BAD] Approx memory diff: {after - before} bytes");
        }

        static void RunOptimized()
        {
            Console.WriteLine("[OPT] CustomerProcessorOptimized");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before = GC.GetTotalMemory(true);

            using (var opt = new CustomerProcessorOptimized())
            {
                opt.Processed += OnProcessed;
                opt.LoadCustomers(100_000);
                opt.ProcessCustomers();
            } // здесь вызывается Dispose, ссылки очищаются

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long after = GC.GetTotalMemory(true);
            Console.WriteLine($"[OPT] Approx memory diff: {after - before} bytes");
        }

        static void OnProcessed(object? sender, EventArgs e) { }
    }

    public class Customer
    {
        public string Name { get; set; } = string.Empty;
    }

    // -------- НЕОПТИМИЗИРОВАННЫЙ ВАРИАНТ --------
    public class CustomerProcessorBad
    {
        private List<Customer> customers = new List<Customer>();
        public event EventHandler? Processed;

        public void LoadCustomers(int count)
        {
            customers = new List<Customer>(count);
            for (int i = 0; i < count; i++)
                customers.Add(new Customer { Name = "Customer " + i });
        }

        public void ProcessCustomers()
        {
            foreach (var c in customers)
            {
                // обработка
            }

            Processed?.Invoke(this, EventArgs.Empty);
            // customers и Processed продолжают держать ссылки на объекты
        }
    }

    // -------- ОПТИМИЗИРОВАННЫЙ ВАРИАНТ --------
    public class CustomerProcessorOptimized : IDisposable
    {
        private List<Customer>? customers = new List<Customer>();
        public event EventHandler? Processed;

        public void LoadCustomers(int count)
        {
            customers = new List<Customer>(count);
            for (int i = 0; i < count; i++)
                customers.Add(new Customer { Name = "Customer " + i });
        }

        public void ProcessCustomers()
        {
            if (customers == null) return;

            foreach (var c in customers)
            {
                // обработка
            }

            Processed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            customers?.Clear();
            customers = null;
            Processed = null; // отпускаем подписчиков
        }
    }
}
