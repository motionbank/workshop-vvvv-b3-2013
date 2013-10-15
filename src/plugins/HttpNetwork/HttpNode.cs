#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using System.IO;
using System.Net;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public enum HTTPProtocol
	{
		GET, HEAD, POST, PUT, DELETE, TRACE, OPTIONS
	}
	
	#region PluginInfo
	[PluginInfo(Name = "HTTP", Category = "Network",
	Help = "", Author = "woei")]
	#endregion PluginInfo
	public class HttpNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		#pragma warning disable 169, 649
		[Input("URL", StringType = StringType.URL, DefaultString = "http://localhost")]
		ISpread<string> FURL;
		
		[Input("Protocol")]
		ISpread<HTTPProtocol> FProtocol;
		
		[Input("Header")]
		ISpread<string> FInHeader;
		
		[Input("Content")]
		ISpread<System.IO.Stream> FContent;
		
		[Input("MimeType", EnumName = "MimeTypeMode")]
		ISpread<EnumEntry> FMimeType;
		
		[Input("Refresh", IsBang = true)]
		ISpread<bool> FRefresh;
		
		
		[Output("Status")]
		ISpread<string> FStatus;
		
		[Output("Header")]
		ISpread<string> FHeader;
		
		[Output("Body")]
		ISpread<System.IO.Stream> FResponse;
		
		[Output("Failed")]
		ISpread<bool> FFailed;
		
		[Output("Success")]
		ISpread<bool> FSuccess;

		[Import()]
		ILogger FLogger;
		#pragma warning restore
		#endregion fields & pins

		public void OnImportsSatisfied()
		{
			FResponse.SliceCount = 0;
		}
		
		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
			FResponse.ResizeAndDispose(spreadMax, () => new MemoryStream());
			FHeader.SliceCount = spreadMax;
			FStatus.SliceCount = spreadMax;
			FFailed.SliceCount = spreadMax;
			FSuccess.SliceCount = spreadMax;
			for (int i=0; i<spreadMax; i++)
			{
				if (FRefresh[i])
				{
					try
					{
						Uri url = new Uri(FURL[i]);
						HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
						request.Method = FProtocol[i].ToString();
						if (!string.IsNullOrEmpty(FInHeader[i]))
							request.Headers.Add(FInHeader[i]);
						request.UserAgent = "vvvv";
						request.Credentials = System.Net.CredentialCache.DefaultCredentials;
						
						byte[] buffer = new byte[1024];
						int bytesRead = 0;
						if (((int)FProtocol[i] == (int)HTTPProtocol.POST) || ((int)FProtocol[i] == (int)HTTPProtocol.PUT))
						{
							request.ContentType = FMimeType[i].Name;
							request.ContentLength = FContent[i].Length;
							using (Stream reqstr = request.GetRequestStream())
							{
								while ((bytesRead = FContent[i].Read(buffer, 0, buffer.Length)) != 0) 
			            				reqstr.Write(buffer, 0, bytesRead);
		        			}
						}
						
						try
						{
						HttpWebResponse response = (HttpWebResponse)request.GetResponse();
						FStatus[i] = string.Format("{0} {1} {2} {3}", url.Scheme.ToUpper(), response.ProtocolVersion, (int)(response.StatusCode), response.StatusDescription);
						FHeader[i] = response.Headers.ToString();
						
						using (Stream respstr = response.GetResponseStream())
						{
							if (response.ContentLength >= 0)
							{
								FResponse[i].SetLength(response.ContentLength);
								while ((bytesRead = respstr.Read(buffer, 0, buffer.Length)) != 0) 
		            					FResponse[i].Write(buffer, 0, bytesRead);
							}
							else
							{
								int bytesTotal = 0;
								FResponse[i].SetLength(0);
								while ((bytesRead = respstr.Read(buffer, 0, buffer.Length)) != 0)
								{
									bytesTotal += bytesRead;
									FResponse[i].SetLength(bytesTotal);
		            				FResponse[i].Write(buffer, 0, bytesRead);
								}
							}
							
						}
						FResponse.Flush(true);
						FFailed[i] = false;
						FSuccess[i] = true;
						}
						catch (WebException we)
						{
							if (we.Status == WebExceptionStatus.ProtocolError)
							{
								var weResp = (HttpWebResponse)we.Response;
								FStatus[i] = string.Format("{0} {1} {2} {3}", url.Scheme.ToUpper(), weResp.ProtocolVersion, (int)(weResp.StatusCode), weResp.StatusDescription);
							}
							else
							{
								FStatus[i] = we.Status.ToString();
							}
							FHeader[i] = string.Empty;
							FContent[i].SetLength(0);
							FFailed[i] = true;
							FSuccess[i] = false;
						}
					}
					catch (Exception e)
					{
						FLogger.Log(e);
						FStatus[i] = e.Message;
						FHeader[i] = string.Empty;
						FContent[i].SetLength(0);
						FFailed[i] = true;
						FSuccess[i] = false;
					}
				}
			}
		}
	}
}
