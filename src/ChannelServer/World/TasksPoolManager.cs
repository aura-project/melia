using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Melia.Channel.World
{

	public class CbFunctionData
	{
		public TasksPoolManager.TimerCallbackFunction cbFunction;
		public Object obj;
	}

	public class TasksPoolManager
	{
		public static readonly TasksPoolManager Instance = new TasksPoolManager();
		public delegate void TimerCallbackFunction(Object obj);
		public List<Timer> generalTimers;
		public Dictionary<CbFunctionData, Timer> timerCallbacks;

		private TasksPoolManager()
		{
			generalTimers = new List<Timer>();
			this.timerCallbacks = new Dictionary<CbFunctionData, Timer>();
		}

		public Timer AddGeneralTask(TimerCallbackFunction action, Object obj, int delay)
		{
			CbFunctionData data = new CbFunctionData();
			data.cbFunction = action;
			data.obj = obj;
			Timer t = new Timer(new TimerCallback(ExecuteTask), data, delay, Timeout.Infinite);

			lock (this.timerCallbacks)
				this.timerCallbacks.Add(data, t);

			this.generalTimers.Add(t);
			return t;
		}

		public Timer AddGeneralTaskAtFixedRate(TimerCallbackFunction action, Object obj, int initial, int delay)
		{
			TimerCallback timerCb = new TimerCallback(action);
			return new Timer(timerCb, obj, initial, delay);
		}

		public void RemoveGeneralTask(Timer t, double delay)
		{
			/// TODO
			/// 

			//t.Change(Timeout.Infinite, Timeout.Infinite);
			t.Dispose();

		}

		public void ExecuteTask(Object obj)
		{
			try
			{
				Timer t;
				lock (this.timerCallbacks)
				{
					CbFunctionData cbFunctionData = (CbFunctionData)obj;
					if (this.timerCallbacks.TryGetValue(cbFunctionData, out t))
					{
						cbFunctionData.cbFunction.Invoke(cbFunctionData.obj);
					}
					t.Dispose();
					this.timerCallbacks.Remove(cbFunctionData);
				}
			}
			catch
			{

			}
		}

		private void TaskMaintenance(Object obj)
		{

		}
	}
}
