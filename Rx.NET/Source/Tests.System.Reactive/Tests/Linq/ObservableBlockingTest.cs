﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.Reactive.Testing;
using Xunit;
using ReactiveTests.Dummies;

#if !NO_TPL
using System.Threading.Tasks;
#endif

namespace ReactiveTests.Tests
{
    
    public partial class ObservableBlockingTest : ReactiveTest
    {
        #region Chunkify

        [Fact]
        public void Chunkify_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Chunkify(default(IObservable<int>)));
        }

        [Fact]
        public void Chunkify_Regular1()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 4),
                OnNext(500, 5),
                OnNext(600, 6),
                OnNext(700, 7),
                OnNext(800, 8),
                OnCompleted<int>(900)
            );

            var ys = xs.Chunkify();
            var e = default(IEnumerator<IList<int>>);

            var res = new List<IList<int>>();

            var log = new Action(() =>
            {
                Assert.True(e.MoveNext());
                res.Add(e.Current);
            });

            scheduler.ScheduleAbsolute(250, () => { e = ys.GetEnumerator(); });
            scheduler.ScheduleAbsolute(270, log);
            scheduler.ScheduleAbsolute(310, log);
            scheduler.ScheduleAbsolute(450, log);
            scheduler.ScheduleAbsolute(470, log);
            scheduler.ScheduleAbsolute(750, log);
            scheduler.ScheduleAbsolute(850, log);
            scheduler.ScheduleAbsolute(950, log);
            scheduler.ScheduleAbsolute(980, () => Assert.False(e.MoveNext()));

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(250, 900)
            );

            Assert.Equal(7, res.Count);
            Assert.True(res[0].SequenceEqual(new int[] { }));
            Assert.True(res[1].SequenceEqual(new int[] { 3 }));
            Assert.True(res[2].SequenceEqual(new int[] { 4 }));
            Assert.True(res[3].SequenceEqual(new int[] { }));
            Assert.True(res[4].SequenceEqual(new int[] { 5, 6, 7 }));
            Assert.True(res[5].SequenceEqual(new int[] { 8 }));
            Assert.True(res[6].SequenceEqual(new int[] { }));
        }

        [Fact]
        public void Chunkify_Regular2()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 4),
                OnNext(500, 5),
                OnNext(600, 6),
                OnNext(700, 7),
                OnNext(800, 8),
                OnCompleted<int>(900)
            );

            var ys = xs.Chunkify();
            var e = default(IEnumerator<IList<int>>);

            var res = new List<IList<int>>();

            var log = new Action(() =>
            {
                Assert.True(e.MoveNext());
                res.Add(e.Current);
            });

            scheduler.ScheduleAbsolute(250, () => { e = ys.GetEnumerator(); });
            scheduler.ScheduleAbsolute(550, log);
            scheduler.ScheduleAbsolute(950, log);
            scheduler.ScheduleAbsolute(980, () => Assert.False(e.MoveNext()));

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(250, 900)
            );

            Assert.Equal(2, res.Count);
            Assert.True(res[0].SequenceEqual(new int[] { 3, 4, 5 }));
            Assert.True(res[1].SequenceEqual(new int[] { 6, 7, 8 }));
        }

        [Fact]
        public void Chunkify_Error()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 4),
                OnNext(500, 5),
                OnNext(600, 6),
                OnError<int>(700, ex)
            );

            var ys = xs.Chunkify();
            var e = default(IEnumerator<IList<int>>);

            var res = new List<IList<int>>();

            var log = new Action(() =>
            {
                Assert.True(e.MoveNext());
                res.Add(e.Current);
            });

            scheduler.ScheduleAbsolute(250, () => { e = ys.GetEnumerator(); });
            scheduler.ScheduleAbsolute(270, log);
            scheduler.ScheduleAbsolute(310, log);
            scheduler.ScheduleAbsolute(450, log);
            scheduler.ScheduleAbsolute(470, log);
            scheduler.ScheduleAbsolute(750, () =>
            {
                try
                {
                    e.MoveNext();
                    Assert.True(false);
                }
                catch (Exception error)
                {
                    Assert.Same(ex, error);
                }
            });

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(250, 700)
            );

            Assert.Equal(4, res.Count);
            Assert.True(res[0].SequenceEqual(new int[] { }));
            Assert.True(res[1].SequenceEqual(new int[] { 3 }));
            Assert.True(res[2].SequenceEqual(new int[] { 4 }));
            Assert.True(res[3].SequenceEqual(new int[] { }));
        }

        #endregion

        #region Collect

        [Fact]
        public void Collect_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Collect(default(IObservable<int>), () => 0, (x, y) => x));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Collect(someObservable, default(Func<int>), (x, y) => x));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Collect(someObservable, () => 0, default(Func<int, int, int>)));

            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Collect(default(IObservable<int>), () => 0, (x, y) => x, x => x));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Collect(someObservable, default(Func<int>), (x, y) => x, x => x));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Collect(someObservable, () => 0, default(Func<int, int, int>), x => x));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Collect(someObservable, () => 0, (x, y) => x, default(Func<int, int>)));
        }

        [Fact]
        public void Collect_Regular1()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 4),
                OnNext(500, 5),
                OnNext(600, 6),
                OnNext(700, 7),
                OnNext(800, 8),
                OnCompleted<int>(900)
            );

            var ys = xs.Collect(() => 0, (x, y) => x + y);
            var e = default(IEnumerator<int>);

            var res = new List<int>();

            var log = new Action(() =>
            {
                Assert.True(e.MoveNext());
                res.Add(e.Current);
            });

            scheduler.ScheduleAbsolute(250, () => { e = ys.GetEnumerator(); });
            scheduler.ScheduleAbsolute(270, log);
            scheduler.ScheduleAbsolute(310, log);
            scheduler.ScheduleAbsolute(450, log);
            scheduler.ScheduleAbsolute(470, log);
            scheduler.ScheduleAbsolute(750, log);
            scheduler.ScheduleAbsolute(850, log);
            scheduler.ScheduleAbsolute(950, log);
            scheduler.ScheduleAbsolute(980, () => Assert.False(e.MoveNext()));

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(250, 900)
            );

            Assert.Equal(7, res.Count);
            Assert.Equal(res[0], new int[] { }.Sum());
            Assert.Equal(res[1], new int[] { 3 }.Sum());
            Assert.Equal(res[2], new int[] { 4 }.Sum());
            Assert.Equal(res[3], new int[] { }.Sum());
            Assert.Equal(res[4], new int[] { 5, 6, 7 }.Sum());
            Assert.Equal(res[5], new int[] { 8 }.Sum());
            Assert.Equal(res[6], new int[] { }.Sum());
        }

        [Fact]
        public void Collect_Regular2()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 4),
                OnNext(500, 5),
                OnNext(600, 6),
                OnNext(700, 7),
                OnNext(800, 8),
                OnCompleted<int>(900)
            );

            var ys = xs.Collect(() => 0, (x, y) => x + y);
            var e = default(IEnumerator<int>);

            var res = new List<int>();

            var log = new Action(() =>
            {
                Assert.True(e.MoveNext());
                res.Add(e.Current);
            });

            scheduler.ScheduleAbsolute(250, () => { e = ys.GetEnumerator(); });
            scheduler.ScheduleAbsolute(550, log);
            scheduler.ScheduleAbsolute(950, log);
            scheduler.ScheduleAbsolute(980, () => Assert.False(e.MoveNext()));

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(250, 900)
            );

            Assert.Equal(2, res.Count);
            Assert.Equal(res[0], new int[] { 3, 4, 5 }.Sum());
            Assert.Equal(res[1], new int[] { 6, 7, 8 }.Sum());
        }

        [Fact]
        public void Collect_InitialCollectorThrows()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 4),
                OnCompleted<int>(500)
            );

            var ex = new Exception();
            var ys = xs.Collect<int, int>(() => { throw ex; }, (x, y) => x + y);

            var ex_ = default(Exception);

            scheduler.ScheduleAbsolute(250, () =>
            {
                try
                {
                    ys.GetEnumerator();
                }
                catch (Exception err)
                {
                    ex_ = err;
                }
            });

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
            );

            Assert.Same(ex_, ex);
        }

        [Fact]
        public void Collect_SecondCollectorThrows()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 4),
                OnCompleted<int>(500)
            );

            var ex = new Exception();
            var n = 0;
            var ys = xs.Collect<int, int>(() => { if (n++ == 0) return 0; else throw ex; }, (x, y) => x + y);
            var e = default(IEnumerator<int>);

            var ex_ = default(Exception);

            scheduler.ScheduleAbsolute(250, () => e = ys.GetEnumerator());
            scheduler.ScheduleAbsolute(350, () =>
            {
                try
                {
                    e.MoveNext();
                }
                catch (Exception err)
                {
                    ex_ = err;
                }
            });

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(250, 350)
            );

            Assert.Same(ex_, ex);
        }

        [Fact]
        public void Collect_NewCollectorThrows()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 4),
                OnCompleted<int>(500)
            );

            var ex = new Exception();
            var ys = xs.Collect<int, int>(() => 0, (x, y) => x + y, x => { throw ex; });
            var e = default(IEnumerator<int>);

            var ex_ = default(Exception);

            scheduler.ScheduleAbsolute(250, () => e = ys.GetEnumerator());
            scheduler.ScheduleAbsolute(350, () =>
            {
                try
                {
                    e.MoveNext();
                }
                catch (Exception err)
                {
                    ex_ = err;
                }
            });

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(250, 350)
            );

            Assert.Same(ex_, ex);
        }

        [Fact]
        public void Collect_MergeThrows()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 4),
                OnCompleted<int>(500)
            );

            var ex = new Exception();
            var ys = xs.Collect<int, int>(() => 0, (x, y) => { throw ex; });
            var e = default(IEnumerator<int>);

            var ex_ = default(Exception);

            scheduler.ScheduleAbsolute(250, () => { e = ys.GetEnumerator(); });
            scheduler.ScheduleAbsolute(350, () =>
            {
                try
                {
                    e.MoveNext();
                }
                catch (Exception err)
                {
                    ex_ = err;
                }
            });

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(250, 300)
            );

            Assert.Same(ex_, ex);
        }

        #endregion

        #region First

        [Fact]
        public void First_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.First(default(IObservable<int>)));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.First(default(IObservable<int>), _ => true));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.First(DummyObservable<int>.Instance, default(Func<int, bool>)));
        }

        [Fact]
        public void First_Empty()
        {
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Empty<int>().First());
        }

        [Fact]
        public void FirstPredicate_Empty()
        {
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Empty<int>().First(_ => true));
        }

        [Fact]
        public void First_Return()
        {
            var value = 42;
            Assert.Equal(value, Observable.Return<int>(value).First());
        }

        [Fact]
        public void FirstPredicate_Return()
        {
            var value = 42;
            Assert.Equal(value, Observable.Return<int>(value).First(i => i % 2 == 0));
        }

        [Fact]
        public void FirstPredicate_Return_NoMatch()
        {
            var value = 42;
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Return<int>(value).First(i => i % 2 != 0));
        }

        [Fact]
        public void First_Throw()
        {
            var ex = new Exception();

            var xs = Observable.Throw<int>(ex);

            ReactiveAssert.Throws(ex, () => xs.First());
        }

        [Fact]
        public void FirstPredicate_Throw()
        {
            var ex = new Exception();

            var xs = Observable.Throw<int>(ex);

            ReactiveAssert.Throws(ex, () => xs.First(_ => true));
        }

        [Fact]
        public void First_Range()
        {
            var value = 42;
            Assert.Equal(value, Observable.Range(value, 10).First());
        }

        [Fact]
        public void FirstPredicate_Range()
        {
            var value = 42;
            Assert.Equal(46, Observable.Range(value, 10).First(i => i > 45));
        }

        #endregion

        #region FirstOrDefault

        [Fact]
        public void FirstOrDefault_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.FirstOrDefault(default(IObservable<int>)));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.FirstOrDefault(default(IObservable<int>), _ => true));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.FirstOrDefault(DummyObservable<int>.Instance, default(Func<int, bool>)));
        }

        [Fact]
        public void FirstOrDefault_Empty()
        {
            Assert.Equal(default(int), Observable.Empty<int>().FirstOrDefault());
        }

        [Fact]
        public void FirstOrDefaultPredicate_Empty()
        {
            Assert.Equal(default(int), Observable.Empty<int>().FirstOrDefault(_ => true));
        }

        [Fact]
        public void FirstOrDefault_Return()
        {
            var value = 42;
            Assert.Equal(value, Observable.Return<int>(value).FirstOrDefault());
        }

        [Fact]
        public void FirstOrDefault_Throw()
        {
            var ex = new Exception();

            var xs = Observable.Throw<int>(ex);

            ReactiveAssert.Throws(ex, () => xs.FirstOrDefault());
        }

        [Fact]
        public void FirstOrDefault_Range()
        {
            var value = 42;
            Assert.Equal(value, Observable.Range(value, 10).FirstOrDefault());
        }

#if !NO_THREAD
        [Fact]
        public void FirstOrDefault_NoDoubleSet()
        {
            //
            // Regression test for a possible race condition caused by Return style operators
            // that could trigger two Set calls on a ManualResetEvent, causing it to get
            // disposed in between those two calls (cf. FirstOrDefaultInternal). This led
            // to an exception will the following stack trace:
            //
            //    System.ObjectDisposedException: Safe handle has been closed
            //       at System.Runtime.InteropServices.SafeHandle.DangerousAddRef(Boolean& success)
            //       at System.StubHelpers.StubHelpers.SafeHandleAddRef(SafeHandle pHandle, Boolean& success)
            //       at Microsoft.Win32.Win32Native.SetEvent(SafeWaitHandle handle)
            //       at System.Threading.EventWaitHandle.Set()
            //       at System.Reactive.Linq.QueryLanguage.<>c__DisplayClass458_1`1.<FirstOrDefaultInternal>b__2()
            //

            var o = new O();

            Scheduler.Default.Schedule(() =>
            {
                var x = o.FirstOrDefault();
            });

            o.Wait();

            o.Next();

            Thread.Sleep(100); // enough time to let the ManualResetEvent dispose

            o.Done();
        }
#endif

        class O : IObservable<int>
        {
            private readonly ManualResetEvent _event = new ManualResetEvent(false);
            private IObserver<int> _observer;

            public void Wait()
            {
                _event.WaitOne();
            }

            public void Next()
            {
                _observer.OnNext(42);
            }

            public void Done()
            {
                _observer.OnCompleted();
            }

            public IDisposable Subscribe(IObserver<int> observer)
            {
                _observer = observer;
                _event.Set();
                return Disposable.Empty;
            }
        }

#endregion

#region + ForEach +

        [Fact]
        public void ForEach_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.ForEach(default(IObservable<int>), x => { }));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.ForEach(someObservable, default(Action<int>)));

            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.ForEach(default(IObservable<int>), (x, i) => { }));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.ForEach(someObservable, default(Action<int, int>)));
        }

        [Fact]
        public void ForEach_Empty()
        {
            var lst = new List<int>();
            Observable.Empty<int>().ForEach(x => lst.Add(x));
            Assert.True(lst.SequenceEqual(Enumerable.Empty<int>()));
        }

        [Fact]
        public void ForEach_Index_Empty()
        {
            var lstX = new List<int>();
            Observable.Empty<int>().ForEach((x, i) => lstX.Add(x));
            Assert.True(lstX.SequenceEqual(Enumerable.Empty<int>()));
        }

        [Fact]
        public void ForEach_Return()
        {
            var lst = new List<int>();
            Observable.Return(42).ForEach(x => lst.Add(x));
            Assert.True(lst.SequenceEqual(new[] { 42 }));
        }

        [Fact]
        public void ForEach_Index_Return()
        {
            var lstX = new List<int>();
            var lstI = new List<int>();
            Observable.Return(42).ForEach((x, i) => { lstX.Add(x); lstI.Add(i); });
            Assert.True(lstX.SequenceEqual(new[] { 42 }));
            Assert.True(lstI.SequenceEqual(new[] { 0 }));
        }

        [Fact]
        public void ForEach_Throw()
        {
            var ex = new Exception();

            var xs = Observable.Throw<int>(ex);

            ReactiveAssert.Throws(ex, () => xs.ForEach(x => { Assert.True(false); }));
        }

        [Fact]
        public void ForEach_Index_Throw()
        {
            var ex = new Exception();

            var xs = Observable.Throw<int>(ex);

            ReactiveAssert.Throws(ex, () => xs.ForEach((x, i) => { Assert.True(false); }));
        }

        [Fact]
        public void ForEach_SomeData()
        {
            var lstX = new List<int>();
            Observable.Range(10, 10).ForEach(x => lstX.Add(x));
            Assert.True(lstX.SequenceEqual(Enumerable.Range(10, 10)));
        }

        [Fact]
        public void ForEach_Index_SomeData()
        {
            var lstX = new List<int>();
            var lstI = new List<int>();
            Observable.Range(10, 10).ForEach((x, i) => { lstX.Add(x); lstI.Add(i); });
            Assert.True(lstX.SequenceEqual(Enumerable.Range(10, 10)));
            Assert.True(lstI.SequenceEqual(Enumerable.Range(0, 10)));
        }

        [Fact]
        public void ForEach_OnNextThrows()
        {
            var ex = new Exception();

            var xs = Observable.Range(0, 10);

            ReactiveAssert.Throws(ex, () => xs.ForEach(x => { throw ex; }));
        }

        [Fact]
        public void ForEach_Index_OnNextThrows()
        {
            var ex = new Exception();

            var xs = Observable.Range(0, 10);

            ReactiveAssert.Throws(ex, () => xs.ForEach((x, i) => { throw ex; }));
        }

#endregion

#region + GetEnumerator +

        [Fact]
        public void GetEnumerator_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.GetEnumerator(default(IObservable<int>)));
        }

        [Fact]
        public void GetEnumerator_Regular1()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable<int>(
                OnNext(10, 2),
                OnNext(20, 3),
                OnNext(30, 5),
                OnNext(40, 7),
                OnCompleted<int>(50)
            );

            var res = default(IEnumerator<int>);

            scheduler.ScheduleAbsolute(default(object), 100, (self, _) => { res = xs.GetEnumerator(); return Disposable.Empty; });

            var hasNext = new List<bool>();
            var vals = new List<Tuple<long, int>>();
            for (long i = 200; i <= 250; i += 10)
            {
                var t = i;
                scheduler.ScheduleAbsolute(default(object), t, (self, _) =>
                {
                    var b = res.MoveNext();
                    hasNext.Add(b);
                    if (b)
                        vals.Add(new Tuple<long, int>(scheduler.Clock, res.Current));
                    return Disposable.Empty;
                });
            }

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(100, 150)
            );

            Assert.Equal(6, hasNext.Count);
            Assert.True(hasNext.Take(4).All(_ => _));
            Assert.True(hasNext.Skip(4).All(_ => !_));

            Assert.Equal(4, vals.Count);
            Assert.True(vals[0].Item1 == 200 && vals[0].Item2 == 2);
            Assert.True(vals[1].Item1 == 210 && vals[1].Item2 == 3);
            Assert.True(vals[2].Item1 == 220 && vals[2].Item2 == 5);
            Assert.True(vals[3].Item1 == 230 && vals[3].Item2 == 7);
        }

        [Fact]
        public void GetEnumerator_Regular2()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable<int>(
                OnNext(10, 2),
                OnNext(30, 3),
                OnNext(50, 5),
                OnNext(70, 7),
                OnCompleted<int>(90)
            );

            var res = default(IEnumerator<int>);

            scheduler.ScheduleAbsolute(default(object), 100, (self, _) => { res = xs.GetEnumerator(); return Disposable.Empty; });

            var hasNext = new List<bool>();
            var vals = new List<Tuple<long, int>>();
            for (long i = 120; i <= 220; i += 20)
            {
                var t = i;
                scheduler.ScheduleAbsolute(default(object), t, (self, _) =>
                {
                    var b = res.MoveNext();
                    hasNext.Add(b);
                    if (b)
                        vals.Add(new Tuple<long, int>(scheduler.Clock, res.Current));
                    return Disposable.Empty;
                });
            }

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(100, 190)
            );

            Assert.Equal(6, hasNext.Count);
            Assert.True(hasNext.Take(4).All(_ => _));
            Assert.True(hasNext.Skip(4).All(_ => !_));

            Assert.Equal(4, vals.Count);
            Assert.True(vals[0].Item1 == 120 && vals[0].Item2 == 2);
            Assert.True(vals[1].Item1 == 140 && vals[1].Item2 == 3);
            Assert.True(vals[2].Item1 == 160 && vals[2].Item2 == 5);
            Assert.True(vals[3].Item1 == 180 && vals[3].Item2 == 7);
        }

        [Fact]
        public void GetEnumerator_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable<int>(
                OnNext(10, 2),
                OnNext(30, 3),
                OnNext(50, 5),
                OnNext(70, 7),
                OnCompleted<int>(90)
            );

            var res = default(IEnumerator<int>);

            scheduler.ScheduleAbsolute(default(object), 100, (self, _) => { res = xs.GetEnumerator(); return Disposable.Empty; });

            scheduler.ScheduleAbsolute(default(object), 140, (self, _) =>
            {
                Assert.True(res.MoveNext());
                Assert.Equal(2, res.Current);

                Assert.True(res.MoveNext());
                Assert.Equal(3, res.Current);

                res.Dispose();

                return Disposable.Empty;
            });

            scheduler.ScheduleAbsolute(default(object), 160, (self, _) =>
            {
                ReactiveAssert.Throws<ObjectDisposedException>(() => res.MoveNext());
                return Disposable.Empty;
            });

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(100, 140)
            );
        }

#if DESKTOPCLR20 || SILVERLIGHTM7
        class Tuple<T1, T2>
        {
            public Tuple(T1 item1, T2 item2)
            {
                Item1 = item1;
                Item2 = item2;
            }

            public T1 Item1 { get; private set; }
            public T2 Item2 { get; private set; }
        }
#endif

#endregion

#region Last

        [Fact]
        public void Last_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Last(default(IObservable<int>)));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Last(default(IObservable<int>), _ => true));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Last(DummyObservable<int>.Instance, default(Func<int, bool>)));
        }

        [Fact]
        public void Last_Empty()
        {
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Empty<int>().Last());
        }

        [Fact]
        public void LastPredicate_Empty()
        {
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Empty<int>().Last(_ => true));
        }

        [Fact]
        public void Last_Return()
        {
            var value = 42;
            Assert.Equal(value, Observable.Return<int>(value).Last());
        }

        [Fact]
        public void Last_Throw()
        {
            var ex = new Exception();

            var xs = Observable.Throw<int>(ex);

            ReactiveAssert.Throws(ex, () => xs.Last());
        }

        [Fact]
        public void Last_Range()
        {
            var value = 42;
            Assert.Equal(value, Observable.Range(value - 9, 10).Last());
        }

        [Fact]
        public void LastPredicate_Range()
        {
            var value = 42;
            Assert.Equal(50, Observable.Range(value, 10).Last(i => i % 2 == 0));
        }

#endregion

#region LastOrDefault

        [Fact]
        public void LastOrDefault_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.LastOrDefault(default(IObservable<int>)));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.LastOrDefault(default(IObservable<int>), _ => true));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.LastOrDefault(DummyObservable<int>.Instance, default(Func<int, bool>)));
        }

        [Fact]
        public void LastOrDefault_Empty()
        {
            Assert.Equal(default(int), Observable.Empty<int>().LastOrDefault());
        }

        [Fact]
        public void LastOrDefaultPredicate_Empty()
        {
            Assert.Equal(default(int), Observable.Empty<int>().LastOrDefault(_ => true));
        }

        [Fact]
        public void LastOrDefault_Return()
        {
            var value = 42;
            Assert.Equal(value, Observable.Return<int>(value).LastOrDefault());
        }

        [Fact]
        public void LastOrDefault_Throw()
        {
            var ex = new Exception();

            var xs = Observable.Throw<int>(ex);

            ReactiveAssert.Throws(ex, () => xs.LastOrDefault());
        }

        [Fact]
        public void LastOrDefault_Range()
        {
            var value = 42;
            Assert.Equal(value, Observable.Range(value - 9, 10).LastOrDefault());
        }

        [Fact]
        public void LastOrDefaultPredicate_Range()
        {
            var value = 42;
            Assert.Equal(50, Observable.Range(value, 10).LastOrDefault(i => i % 2 == 0));
        }

#endregion

#region + Latest +

        [Fact]
        public void Latest_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Latest(default(IObservable<int>)));
        }

        [Fact]
        public void Latest1()
        {
            var disposed = false;
            var evt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                Task.Run(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    evt.WaitOne();
                    obs.OnNext(2);
                    evt.WaitOne();
                    obs.OnCompleted();
                });

                return () => { disposed = true; };
            });

            var res = src.Latest().GetEnumerator();

            Task.Run(async () =>
            {
                await Task.Delay(250);
                evt.Set();
            });

            Assert.True(res.MoveNext());
            Assert.Equal(1, res.Current);

            evt.Set();
            Assert.True(((IEnumerator)res).MoveNext());
            Assert.Equal(2, ((IEnumerator)res).Current);

            evt.Set();
            Assert.False(res.MoveNext());

            ReactiveAssert.Throws<NotSupportedException>(() => res.Reset());

            res.Dispose();
            //ReactiveAssert.Throws<ObjectDisposedException>(() => res.MoveNext());
            Assert.True(disposed);
        }

        [Fact]
        public void Latest2()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(250, 5),
                OnNext(260, 6),
                OnNext(270, 7),
                OnNext(280, 8),
                OnNext(290, 9),
                OnCompleted<int>(300)
            );

            var res = xs.Latest();

            var e1 = default(IEnumerator<int>);
            scheduler.ScheduleAbsolute(205, () =>
            {
                e1 = res.GetEnumerator();
            });

            var o1 = new List<int>();
            scheduler.ScheduleAbsolute(235, () =>
            {
                Assert.True(e1.MoveNext());
                o1.Add(e1.Current);
            });
            scheduler.ScheduleAbsolute(265, () =>
            {
                Assert.True(e1.MoveNext());
                o1.Add(e1.Current);
            });

            scheduler.ScheduleAbsolute(285, () => e1.Dispose());

            var e2 = default(IEnumerator);
            scheduler.ScheduleAbsolute(255, () =>
            {
                e2 = ((IEnumerable)res).GetEnumerator();
            });

            var o2 = new List<int>();
            scheduler.ScheduleAbsolute(265, () =>
            {
                Assert.True(e2.MoveNext());
                o2.Add((int)e2.Current);
            });
            scheduler.ScheduleAbsolute(275, () =>
            {
                Assert.True(e2.MoveNext());
                o2.Add((int)e2.Current);
            });

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(205, 285),
                Subscribe(255, 300)
            );

            o1.AssertEqual(3, 6);
            o2.AssertEqual(6, 7);
        }

        [Fact]
        public void Latest_Error()
        {
            SynchronizationContext.SetSynchronizationContext(null);

            var ex = new Exception();

            var evt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                Task.Run(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    evt.WaitOne();
                    obs.OnError(ex);
                });

                return () => { };
            });

            var res = src.Latest().GetEnumerator();

            Task.Run(async () =>
            {
                await Task.Delay(250);
                evt.Set();
            });

            Assert.True(res.MoveNext());
            Assert.Equal(1, res.Current);

            evt.Set();

            ReactiveAssert.Throws(ex, () => res.MoveNext());
        }

#endregion

#region + MostRecent +

        [Fact]
        public void MostRecent_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.MostRecent(default(IObservable<int>), 1));
        }

        [Fact]
        public void MostRecent1()
        {
            var evt = new AutoResetEvent(false);
            var nxt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                Task.Run(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    nxt.Set();
                    evt.WaitOne();
                    obs.OnNext(2);
                    nxt.Set();
                    evt.WaitOne();
                    obs.OnCompleted();
                    nxt.Set();
                });

                return () => { };
            });

            var res = src.MostRecent(42).GetEnumerator();

            Assert.True(res.MoveNext());
            Assert.Equal(42, res.Current);
            Assert.True(res.MoveNext());
            Assert.Equal(42, res.Current);

            for (int i = 1; i <= 2; i++)
            {
                evt.Set();
                nxt.WaitOne();
                Assert.True(res.MoveNext());
                Assert.Equal(i, res.Current);
                Assert.True(res.MoveNext());
                Assert.Equal(i, res.Current);
            }

            evt.Set();
            nxt.WaitOne();
            Assert.False(res.MoveNext());
        }

        [Fact]
        public void MostRecent2()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(250, 5),
                OnNext(260, 6),
                OnNext(270, 7),
                OnNext(280, 8),
                OnNext(290, 9),
                OnCompleted<int>(300)
            );

            var res = xs.MostRecent(0);

            var e1 = default(IEnumerator<int>);
            scheduler.ScheduleAbsolute(200, () =>
            {
                e1 = res.GetEnumerator();
            });

            var o1 = new List<int>();
            scheduler.ScheduleAbsolute(205, () =>
            {
                Assert.True(e1.MoveNext());
                o1.Add(e1.Current);
            });
            scheduler.ScheduleAbsolute(232, () =>
            {
                Assert.True(e1.MoveNext());
                o1.Add(e1.Current);
            });
            scheduler.ScheduleAbsolute(234, () =>
            {
                Assert.True(e1.MoveNext());
                o1.Add(e1.Current);
            });
            scheduler.ScheduleAbsolute(265, () =>
            {
                Assert.True(e1.MoveNext());
                o1.Add(e1.Current);
            });

            scheduler.ScheduleAbsolute(285, () => e1.Dispose());

            var e2 = default(IEnumerator);
            scheduler.ScheduleAbsolute(255, () =>
            {
                e2 = ((IEnumerable)res).GetEnumerator();
            });

            var o2 = new List<int>();
            scheduler.ScheduleAbsolute(258, () =>
            {
                Assert.True(e2.MoveNext());
                o2.Add((int)e2.Current);
            });
            scheduler.ScheduleAbsolute(262, () =>
            {
                Assert.True(e2.MoveNext());
                o2.Add((int)e2.Current);
            });
            scheduler.ScheduleAbsolute(264, () =>
            {
                Assert.True(e2.MoveNext());
                o2.Add((int)e2.Current);
            });
            scheduler.ScheduleAbsolute(275, () =>
            {
                Assert.True(e2.MoveNext());
                o2.Add((int)e2.Current);
            });

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 285),
                Subscribe(255, 300)
            );

            o1.AssertEqual(0, 3, 3, 6);
            o2.AssertEqual(0, 6, 6, 7);
        }

        [Fact]
        public void MostRecent_Error()
        {
            var ex = new Exception();

            var evt = new AutoResetEvent(false);
            var nxt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                Task.Run(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    nxt.Set();
                    evt.WaitOne();
                    obs.OnError(ex);
                    nxt.Set();
                });

                return () => { };
            });

            var res = src.MostRecent(42).GetEnumerator();

            Assert.True(res.MoveNext());
            Assert.Equal(42, res.Current);
            Assert.True(res.MoveNext());
            Assert.Equal(42, res.Current);

            evt.Set();
            nxt.WaitOne();
            Assert.True(res.MoveNext());
            Assert.Equal(1, res.Current);
            Assert.True(res.MoveNext());
            Assert.Equal(1, res.Current);

            evt.Set();
            nxt.WaitOne();

            ReactiveAssert.Throws(ex, () => res.MoveNext());
        }

#endregion

#region + Next +

        [Fact]
        public void Next_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Next(default(IObservable<int>)));
        }

        [Fact]
        public void Next1()
        {
            var evt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                Task.Run(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    evt.WaitOne();
                    obs.OnNext(2);
                    evt.WaitOne();
                    obs.OnCompleted();
                });

                return () => { };
            });

            var res = src.Next().GetEnumerator();

            Action release = () => Task.Run(async () =>
            {
                await Task.Delay(250);
                evt.Set();
            });

            release();
            Assert.True(res.MoveNext());
            Assert.Equal(1, res.Current);

            release();
            Assert.True(res.MoveNext());
            Assert.Equal(2, res.Current);

            release();
            Assert.False(res.MoveNext());
        }


        [Fact]
        public void Next2()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(250, 5),
                OnNext(260, 6),
                OnNext(270, 7),
                OnNext(280, 8),
                OnNext(290, 9),
                OnCompleted<int>(300)
            );

            var res = xs.Next();

            var e1 = default(IEnumerator<int>);
            scheduler.ScheduleAbsolute(200, () =>
            {
                e1 = res.GetEnumerator();
            });

            scheduler.ScheduleAbsolute(285, () => e1.Dispose());

            var e2 = default(IEnumerator);
            scheduler.ScheduleAbsolute(255, () =>
            {
                e2 = ((IEnumerable)res).GetEnumerator();
            });

            scheduler.Start();

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 285),
                Subscribe(255, 300)
            );
        }

        [Fact]
        public void Next_DoesNotBlock()
        {
            var evt = new ManualResetEvent(false);

            var xs = Observable.Empty<int>().Do(_ => { }, () => evt.Set());

            var e = xs.Next().GetEnumerator();

            evt.WaitOne();

            Assert.False(e.MoveNext());
        }

        [Fact]
        public void Next_SomeResults()
        {
            var xs = Observable.Range(0, 100, Scheduler.Default);

            var res = xs.Next().ToList();

            Assert.True(res.All(x => x < 100));
            Assert.True(res.Count == res.Distinct().Count());
        }

#if !NO_THREAD
        [Fact]
        public void Next_Error()
        {
            var ex = new Exception();

            var evt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                new Thread(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    evt.WaitOne();
                    obs.OnError(ex);
                }).Start();

                return () => { };
            });

            var res = src.Next().GetEnumerator();

            Action release = () => new Thread(() =>
            {
                Thread.Sleep(250);
                evt.Set();
            }).Start();

            release();
            Assert.True(res.MoveNext());
            Assert.Equal(1, res.Current);

            release();

            ReactiveAssert.Throws(ex, () => res.MoveNext());
        }
#endif
#endregion

#region Single

        [Fact]
        public void Single_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Single(default(IObservable<int>)));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Single(default(IObservable<int>), _ => true));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Single(DummyObservable<int>.Instance, default(Func<int, bool>)));
        }

        [Fact]
        public void Single_Empty()
        {
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Empty<int>().Single());
        }

        [Fact]
        public void SinglePredicate_Empty()
        {
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Empty<int>().Single(_ => true));
        }

        [Fact]
        public void Single_Return()
        {
            var value = 42;
            Assert.Equal(value, Observable.Return<int>(value).Single());
        }

        [Fact]
        public void Single_Throw()
        {
            var ex = new Exception();

            var xs = Observable.Throw<int>(ex);

            ReactiveAssert.Throws(ex, () => xs.Single());
        }

        [Fact]
        public void Single_Range()
        {
            var value = 42;
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Range(value, 10).Single());
        }

        [Fact]
        public void SinglePredicate_Range()
        {
            var value = 42;
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Range(value, 10).Single(i => i % 2 == 0));
        }

        [Fact]
        public void SinglePredicate_Range_ReducesToSingle()
        {
            var value = 42;
            Assert.Equal(45, Observable.Range(value, 10).Single(i => i == 45));
        }

#endregion

#region SingleOrDefault

        [Fact]
        public void SingleOrDefault_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.SingleOrDefault(default(IObservable<int>)));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.SingleOrDefault(default(IObservable<int>), _ => true));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.SingleOrDefault(DummyObservable<int>.Instance, default(Func<int, bool>)));
        }

        [Fact]
        public void SingleOrDefault_Empty()
        {
            Assert.Equal(default(int), Observable.Empty<int>().SingleOrDefault());
        }

        [Fact]
        public void SingleOrDefaultPredicate_Empty()
        {
            Assert.Equal(default(int), Observable.Empty<int>().SingleOrDefault(_ => true));
        }

        [Fact]
        public void SingleOrDefault_Return()
        {
            var value = 42;
            Assert.Equal(value, Observable.Return<int>(value).SingleOrDefault());
        }

        [Fact]
        public void SingleOrDefault_Throw()
        {
            var ex = new Exception();

            var xs = Observable.Throw<int>(ex);

            ReactiveAssert.Throws(ex, () => xs.SingleOrDefault());
        }

        [Fact]
        public void SingleOrDefault_Range()
        {
            var value = 42;
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Range(value, 10).SingleOrDefault());
        }

        [Fact]
        public void SingleOrDefaultPredicate_Range()
        {
            var value = 42;
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Range(value, 10).SingleOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void SingleOrDefault_Range_ReducesToSingle()
        {
            var value = 42;
            Assert.Equal(45, Observable.Range(value, 10).SingleOrDefault(i => i == 45));
        }

        [Fact]
        public void SingleOrDefault_Range_ReducesToNone()
        {
            var value = 42;
            Assert.Equal(0, Observable.Range(value, 10).SingleOrDefault(i => i > 100));
        }

#endregion

#region Wait

        [Fact]
        public void Wait_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Wait(default(IObservable<int>)));
        }

#if !NO_THREAD
        [Fact]
        public void Wait_Return()
        {
            var x = 42;
            var xs = Observable.Return(x, ThreadPoolScheduler.Instance);
            var res = xs.Wait();
            Assert.Equal(x, res);
        }
#endif

        [Fact]
        public void Wait_Empty()
        {
            ReactiveAssert.Throws<InvalidOperationException>(() => Observable.Empty<int>().Wait());
        }

        [Fact]
        public void Wait_Throw()
        {
            var ex = new Exception();

            var xs = Observable.Throw<int>(ex);

            ReactiveAssert.Throws(ex, () => xs.Wait());
        }

#if !NO_THREAD
        [Fact]
        public void Wait_Range()
        {
            var n = 42;
            var xs = Observable.Range(1, n, ThreadPoolScheduler.Instance);
            var res = xs.Wait();
            Assert.Equal(n, res);
        }
#endif
#endregion
    }
}
