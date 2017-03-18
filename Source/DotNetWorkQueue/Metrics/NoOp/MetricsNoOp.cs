// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
namespace DotNetWorkQueue.Metrics.NoOp
{
    internal class MeterNoOp : IMeter
    {
        public void Mark()
        {
           
        }

        public void Mark(string item)
        {
           
        }

        public void Mark(long count)
        {
            
        }

        public void Mark(string item, long count)
        {
            
        }
    }
    internal class MetricsContextNoOp : IMetricsContext
    {
        private readonly ITimer _timer = new TimerNoOp();
        private readonly ICounter _counter = new CounterNoOp();
        private readonly IHistogram _histogram = new HistogramNoOp();
        private readonly IMeter _meter = new MeterNoOp();

        public IMetricsContext Context(string contextName)
        {
            return new MetricsContextNoOp();
        }

        public void ShutdownContext(string contextName)
        {
            
        }

        public void Gauge(string name, Func<double> valueProvider, Units unit, string tag = null)
        {
            
        }

        public ICounter Counter(string name, Units unit, string tag = null)
        {
            return _counter;
        }

        public ICounter Counter(string name, string unitName, string tag = null)
        {
            return _counter;
        }

        public IMeter Meter(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, string tag = null)
        {
            return _meter;
        }

        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, string tag = null)
        {
            return _meter;
        }

        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, string tag = null)
        {
            return _histogram;
        }

        public ITimer Timer(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, TimeUnits rateUnit = TimeUnits.Seconds, TimeUnits durationUnit = TimeUnits.Milliseconds, string tag = null)
        {
            return _timer;
        }

        public void Dispose()
        {
           
        }
    }
    internal class TimerContextNoOp : ITimerContext
    {
        public TimeSpan Elapsed => TimeSpan.Zero;

        public void Dispose()
        {
           
        }
    }
    internal class TimerNoOp : ITimer, IDisposable
    {
        private readonly ITimerContext _timerContext = new TimerContextNoOp();

        public void Record(long time, TimeUnits unit, string userValue = null)
        {
           
        }

        public void Time(Action action, string userValue = null)
        {
           
        }

        public T Time<T>(Func<T> action, string userValue = null)
        {
            return action();
        }

        public ITimerContext NewContext(string userValue = null)
        {
            return _timerContext;
        }

        public ITimerContext NewContext(Action<TimeSpan> finalAction, string userValue = null)
        {
            return _timerContext;
        }

        public void Dispose()
        {
            _timerContext.Dispose();
        }
    }
    internal class HistogramNoOp : IHistogram
    {
        public void Update(long value, string userValue = null)
        {
            
        }
    }
    internal class CounterNoOp : ICounter
    {

        public void Increment()
        {
            
        }

        public void Increment(string item)
        {
            
        }

        public void Increment(long amount)
        {
            
        }

        public void Increment(string item, long amount)
        {
           
        }

        public void Decrement()
        {
            
        }

        public void Decrement(string item)
        {
           
        }

        public void Decrement(long amount)
        {
            
        }

        public void Decrement(string item, long amount)
        {
            
        }
    }
    internal class MetricsNoOp: IMetrics, IDisposable
    {
        private readonly ITimer _timer = new TimerNoOp();
        private readonly ICounter _counter = new CounterNoOp();
        private readonly IHistogram _histogram = new HistogramNoOp();
        private readonly IMeter _meter = new MeterNoOp();
        private readonly IMetricsContext _metricsContext = new MetricsContextNoOp();

        public IMetricsContext Context(string contextName)
        {
            return _metricsContext;
        }

        public void ShutdownContext(string contextName)
        {
            
        }

        public void Gauge(string name, Func<double> valueProvider, Units unit, string tag = null)
        {
           
        }

        public IMeter Meter(string name, Units unit, TimeUnits rateUnit, string tag = null)
        {
            return _meter;
        }

        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, string tag = null)
        {
            return _meter;
        }

        public ICounter Counter(string name, Units unit, string tag = null)
        {
            return _counter;
        }

        public ICounter Counter(string name, string unitName, string tag = null)
        {
            return _counter;
        }

        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType, string tag = null)
        {
            return _histogram;
        }

        public ITimer Timer(string name, Units unit, SamplingTypes samplingType, TimeUnits rateUnit, TimeUnits durationUnit, string tag = null)
        {
            return _timer;
        }

        public dynamic CollectedMetrics => null;

        public void Dispose()
        {
            _metricsContext.Dispose();
        }
    }
}
