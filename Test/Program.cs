﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetSerializer;
using System.IO;
using System.Diagnostics;

namespace Test
{
	interface INetTest
	{
		string Framework { get; }
		void Prepare(int numMessages);
		MessageBase[] Test(MessageBase[] msgs);
	}

	interface IMemStreamTest
	{
		string Framework { get; }
		void Prepare(int numMessages);
		long Serialize(MessageBase[] msgs);
		MessageBase[] Deserialize();
	}

	class Program
	{
		static void Main(string[] args)
		{
			System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

			var types = typeof(MessageBase).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MessageBase))).ToArray();

			Serializer.Initialize(types);

			Warmup();

			MessageBase[] msgs;

			msgs = MessageBase.CreateSimpleMessages(2000000).ToArray();
			RunTests("SimpleMessages", msgs);

			msgs = MessageBase.CreateMessages(300000).ToArray();
			RunTests("Messages", msgs);

			msgs = MessageBase.CreateLongMessages(500).ToArray();
			RunTests("LongMessages", msgs);

			Console.WriteLine("Press enter to quit");
			Console.ReadLine();
		}

		static void Warmup()
		{
			var msgs = new MessageBase[] { new SimpleMessage(), new Message(), new LongMessage() };

			IMemStreamTest t;

			t = new MemStreamTest();
			t.Prepare(msgs.Length);
			t.Serialize(msgs);
			t.Deserialize();

			t = new PBMemStreamTest();
			t.Prepare(msgs.Length);
			t.Serialize(msgs);
			t.Deserialize();
		}

		static void RunTests(string name, MessageBase[] msgs)
		{
			Console.WriteLine("== {0} {1} ==", msgs.Length, name);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Test(new MemStreamTest(), msgs);
			Test(new PBMemStreamTest(), msgs);

			Test(new NetTest(), msgs);
			Test(new PBNetTest(), msgs);
		}

		static void Test(IMemStreamTest test, MessageBase[] msgs)
		{
			test.Prepare(msgs.Length);

			/* Serialize part */
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var c0 = GC.CollectionCount(0);
				var c1 = GC.CollectionCount(1);
				var c2 = GC.CollectionCount(2);

				var sw = Stopwatch.StartNew();

				long size = test.Serialize(msgs);

				sw.Stop();

				c0 = GC.CollectionCount(0) - c0;
				c1 = GC.CollectionCount(1) - c1;
				c2 = GC.CollectionCount(2) - c2;

				Console.WriteLine("{0,-13} | {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
					test.Framework, "MemStream Serialize", sw.ElapsedMilliseconds, c0, c1, c2, size);
			}

			/* Deerialize part */

			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var c0 = GC.CollectionCount(0);
				var c1 = GC.CollectionCount(1);
				var c2 = GC.CollectionCount(2);

				var sw = Stopwatch.StartNew();

				var received = test.Deserialize();

				sw.Stop();

				c0 = GC.CollectionCount(0) - c0;
				c1 = GC.CollectionCount(1) - c1;
				c2 = GC.CollectionCount(2) - c2;

				Console.WriteLine("{0,-13} | {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
					test.Framework, "MemStream Deserialize", sw.ElapsedMilliseconds, c0, c1, c2, "");

				for (int i = 0; i < msgs.Length; ++i)
				{
					var msg1 = msgs[i];
					var msg2 = received[i];

					msg1.Compare(msg2);
				}
			}
		}

		static void Test(INetTest test, MessageBase[] msgs)
		{
			test.Prepare(msgs.Length);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var c0 = GC.CollectionCount(0);
			var c1 = GC.CollectionCount(1);
			var c2 = GC.CollectionCount(2);

			var sw = Stopwatch.StartNew();

			var received = test.Test(msgs);

			sw.Stop();

			c0 = GC.CollectionCount(0) - c0;
			c1 = GC.CollectionCount(1) - c1;
			c2 = GC.CollectionCount(2) - c2;

			Console.WriteLine("{0,-13} | {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
				test.Framework, "NetTest", sw.ElapsedMilliseconds, c0, c1, c2, "");

			for (int i = 0; i < msgs.Length; ++i)
			{
				var msg1 = msgs[i];
				var msg2 = received[i];

				msg1.Compare(msg2);
			}
		}
	}
}