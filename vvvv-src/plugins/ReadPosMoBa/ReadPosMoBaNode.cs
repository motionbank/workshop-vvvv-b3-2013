#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Streams;
using System.IO;
using System.Collections.Generic;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "ReadPos", Category = "Value", Version = "MoBa", Help = "Reads MoBa-data-files per line and converts directly to values", Author = "woei")]
	#endregion PluginInfo
	public class ReadPosMoBaNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
	{
		#region fields & pins
		[Input("Filename", StringType = StringType.Filename)]
		public IDiffSpread<string> FFile;
		
		[Input("Index", MinValue = 0)]
		public IDiffSpread<int> FId;
		
		[Input("Count", DefaultValue = 1, MinValue = 1)]
		public IDiffSpread<int> FCount;

		[Output("Output")]
		public ISpread<double> FOutput;
		
		[Output("Line Index")]
		public ISpread<int> FLineId;
		
		[Output("Line Count", AllowFeedback = true)]
		public ISpread<int> FLineCount;

		[Import()]
		public ILogger FLogger;
		
		private Spread<LineReader> FReader;
		#endregion fields & pins

		#region IPartImportsSatisfiedNotification
		public void OnImportsSatisfied()
		{
			FReader = new Spread<LineReader>(0);
		}
		#endregion
		
		#region IDisposable
		public bool disposed = false;
		
		public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!this.disposed)
            {
                if(disposing)
                {
                   FReader.ResizeAndDispose(0, () => new LineReader(string.Empty));
                }
                disposed = true;
            }
        }
		
		~ReadPosMoBaNode()
        {
            Dispose(false);
        }
		#endregion
		
		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
			FReader.ResizeAndDispose(spreadMax, (slice) => new LineReader(FFile[slice]));

			if (FFile.IsChanged || FId.IsChanged || FCount.IsChanged)
			{
				FOutput.SliceCount=0;
				FLineId.SliceCount=0;
				FLineCount.SliceCount = 0;
				for (int i=0; i<spreadMax; i++)
				{
					FLineCount.Add(FReader[i].LineCount);
					if (FFile.IsChanged)
					{
						FReader[i].Dispose();
						FReader[i] = new LineReader(FFile[i]);
						
					}
					foreach (string line in FReader[i].ReadLines(FId[i],FCount[i]))
					{
						string[] components = line.Split(' ');
						foreach (string c in components)
						{
							try
							{
								FOutput.Add(double.Parse(c.Replace(".",",")));
							}
							catch
							{
								FLogger.Log(LogType.Debug, c);
							}
						}
							
					}
					FLineId.Add(FReader[i].LineIndex);
				}
			}
		}
		
		private class LineReader : StreamReader
		{
			public int LineIndex;
			public int LineCount;
			private List<long> lineStart;
			private bool valid;
			
			private List<string> buffer;
			public LineReader(string filename):base(filename)
			{
				LineIndex = -1;
				LineCount = 0;
				lineStart = new List<long>();
				
				valid = this.BaseStream != null;
				
				
					if (valid)
					{
						int pos = 0;
						this.BaseStream.Position = 0;
						while (!this.EndOfStream)
						{
							lineStart.Add(pos);
							pos += this.ReadLine().Length;
							LineCount++;
						}
						ResetPosition();
					}
				
				buffer = new List<string>();
			}
			
			public List<string> ReadLines(int index, int count)
			{
				if (this.BaseStream == null)
				{
					buffer.Clear();
					return buffer;
				}
				else
				{
					int id = index%LineCount;
					if (id < LineIndex)
					{
						this.BaseStream.Seek(0, SeekOrigin.Begin);
						this.DiscardBufferedData();
						int incr = 0;
						while (incr<id)
						{
							incr++;
							this.ReadLine();
						}
						
						buffer.Clear();
						LineIndex = id;
						for (int i=0; i<count; i++)
						{
							if (this.EndOfStream)
							{
								ResetPosition();
							}
							
							buffer.Add(this.ReadLine());
						}
					}
					else
					{
						if ((id == LineIndex) && (buffer.Count != count))
						{
							if (buffer.Count > count)
								buffer = buffer.GetRange(0,count);
							else
							{
								for (int j=buffer.Count; j<count; j++)
								{
									buffer.Add(this.ReadLine());
								}
							}
						}
						else if (id > LineIndex)
						{
							int diff =(LineIndex+buffer.Count)-id;
							if (diff>0)
							{
								int remCount = buffer.Count-diff;
								for (int k=0; k<remCount; k++)
									buffer.RemoveAt(0);
								for (int l=0; l<count-diff; l++)
									buffer.Add(this.ReadLine());
								
								buffer = buffer.GetRange(0,count);
							}
							else
							{
								buffer.Clear();
								for (int m=0; m>diff; m--)
									this.ReadLine();
								for (int n=0; n<count; n++)
									buffer.Add(this.ReadLine());
							}
							LineIndex = id;
						}
					}
					return buffer;
					
				}
			}
			
			public void ResetPosition()
			{
				LineIndex = 0;
				this.BaseStream.Seek(0, SeekOrigin.Begin);
				this.DiscardBufferedData();
			}
			
			public override string ReadLine()
			{
				if (this.EndOfStream)
					ResetPosition();
				return base.ReadLine();
			}
		}
	}
}
