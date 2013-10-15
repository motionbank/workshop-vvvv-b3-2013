#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using VVVV.Core.Logging;
using System.Web.Script.Serialization;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "AsXElement", Category = "Raw", Version = "", Help = "Parses Raw JSON to XDocument", Tags = "xml", Author = "woei")]
	#endregion PluginInfo
	public class RawAsXElementNode : IPluginEvaluate
	{
		#pragma warning disable 649, 169
		#region fields & pins
		[Input("Input")]
		IDiffSpread<System.IO.Stream> FInput;
		
		[Input("Include Root", DefaultBoolean = true)]
		IDiffSpread<bool> FRoot;

		[Output("Element")]
		ISpread<XElement> FElement;

		[Output("Document")]
		ISpread<XDocument> FDocument;
		
		[Output("Success")]
		ISpread<bool> FSuccess;
		
		[Import()]
		ILogger FLogger;
		#endregion fields & pins
		#pragma warning restore

		public void Evaluate(int SpreadMax)
		{
			if (FInput.IsChanged || FRoot.IsChanged)
			{
				FElement.SliceCount = SpreadMax;
				FDocument.SliceCount = SpreadMax;
				FSuccess.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
			    	using (var reader = JsonReaderWriterFactory.CreateJsonReader(FInput[i], new XmlDictionaryReaderQuotas()))
			        {
			        	try
				    	{
				        	var doc = XDocument.Load(reader);
				        	XmlNameTable nameTable = reader.NameTable;
				        	var nsmgr = new XmlNamespaceManager(nameTable);
				        	nsmgr.AddNamespace("a","item");
				        	Spread<XElement> matches = doc.XPathSelectElements("//a:item", nsmgr).ToSpread();
				        	for (int m = matches.SliceCount-1; m>=0; m--)
				        	{
				        		var n = matches[m];
								var node = new XElement(n.Attribute("item").Value);
			        			foreach (var c in n.Elements())
				        				node.Add(c);
				        		foreach (var a in n.Attributes())
				        			if ((a.Name.LocalName != "a") && (a.Name.LocalName != "item"))
				        				node.Add(a);
				        		if (!n.HasElements)
				        			node.Value = n.Value;
				        		n.ReplaceWith(node);
				        	}
				        	if (!FRoot[i])
				        		doc.Root.ReplaceWith(doc.Root.FirstNode);
				        	
				        	FDocument[i] = doc;
				        	FElement[i] = doc.Root;
				        	FSuccess[i] = true;
				    	}
				    	catch (Exception e)
				    	{
				    		FDocument[i] = new XDocument(new XElement("error"));
				    		FElement[i] = FDocument[i].Root;
				    		FSuccess[i] = false;
				    		FLogger.Log(e);
				    	}
			        }
			    	
				}
			}
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "AsXElement", Category = "String", Version = "JSON", Help = "Parses JSON strings to XDocument", Tags = "xml", Author = "woei")]
	#endregion PluginInfo
	public class JSONAsXElementNode : IPluginEvaluate
	{
		#pragma warning disable 649, 169
		#region fields & pins
		[Input("JSON", DefaultString = "{\"vvvv\":\"awesome\"}")]
		IDiffSpread<string> FInput;
		
		[Input("Include Root", DefaultBoolean = true)]
		IDiffSpread<bool> FRoot;

		[Output("Element")]
		ISpread<XElement> FElement;

		[Output("Document")]
		ISpread<XDocument> FDocument;
		
		[Output("Success")]
		ISpread<bool> FSuccess;
		
		[Import()]
		ILogger FLogger;
		#endregion fields & pins
		#pragma warning restore

		public void Evaluate(int SpreadMax)
		{
			if (FInput.IsChanged || FRoot.IsChanged)
			{
				FElement.SliceCount = SpreadMax;
				FDocument.SliceCount = SpreadMax;
				FSuccess.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
					var buffer = System.Text.UTF8Encoding.UTF8.GetBytes(FInput[i]);    
					
			    	using (var reader = JsonReaderWriterFactory.CreateJsonReader(buffer, new XmlDictionaryReaderQuotas()))
			        {
			        	try
			    	{
			        	var doc = XDocument.Load(reader);
			        	XmlNameTable nameTable = reader.NameTable;
			        	var nsmgr = new XmlNamespaceManager(nameTable);
			        	nsmgr.AddNamespace("a","item");
			        	Spread<XElement> matches = doc.XPathSelectElements("//a:item", nsmgr).ToSpread();
			        	for (int m = matches.SliceCount-1; m>=0; m--)
			        	{
			        		var n = matches[m];
							var node = new XElement(n.Attribute("item").Value);
		        			foreach (var c in n.Elements())
			        				node.Add(c);
			        		foreach (var a in n.Attributes())
			        			if ((a.Name.LocalName != "a") && (a.Name.LocalName != "item"))
			        				node.Add(a);
			        		if (!n.HasElements)
			        			node.Value = n.Value;
			        		n.ReplaceWith(node);
			        	}
			        	if (!FRoot[i])
			        		doc.Root.ReplaceWith(doc.Root.FirstNode);
			        	
			        	
			        	FDocument[i] = doc;
			        	FElement[i] = doc.Root;
			        	FSuccess[i] = true;
			    		}
			    	catch (Exception e)
			    	{
			    		FDocument[i] = new XDocument(new XElement("error"));
			    		FElement[i] = FDocument[i].Root;
			    		FSuccess[i] = false;
			    		FLogger.Log(e);
			    	}
			        }
			    	
				}
			}
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "AsString", Category = "Object", Version = "JSON", Help = "Serializes XElements to JSON string", Tags = "xml", Author = "woei")]
	#endregion PluginInfo
	public class JSONObjectAsStringNode : IPluginEvaluate
	{
		#pragma warning disable 649, 169
		#region fields & pins
		[Input("Element")]
		IDiffSpread<XElement> FInput;

		[Output("String")]
		ISpread<string> FOutput;

		[Import()]
		ILogger FLogger;
		
		#endregion fields & pins
		#pragma warning restore

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FInput.IsChanged)
			{
				FOutput.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
					try
					{
					    using (var stream = new MemoryStream())
					    {
					    	using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, new System.Text.UTF8Encoding(), true))
					        {
					        	XDocument doc;
					        	if (FInput[i] == null)
					        		doc = new XDocument(new XElement("root"));
					        	else if (FInput[i].Name.LocalName == "root")
					        		doc = new XDocument(FInput[i]);
					        	else
					        	{
					        		var root = new XElement("root");
					        		root.Add(new XAttribute("type","object"));
					        		root.Add(FInput[i]);
					        		doc = new XDocument(root);
					        	}
					        	
					        	
					        	Spread<XElement> matches = doc.XPathSelectElements("//*[not(@type)]").ToSpread();
					        	for (int m=matches.SliceCount-1; m>=0; m--)
					        	{
					        		if (matches[m].HasElements)
					        			matches[m].Add(new XAttribute("type","object"));
					        		else
					        			matches[m].Add(new XAttribute("type","string"));
					        	}
					        	doc.WriteTo(writer);
					        }
					        FOutput[i] = System.Text.Encoding.UTF8.GetString(stream.ToArray());
					    }
					}
					catch (Exception e)
					{
						FOutput[i] = "{\"error\":\""+e.Message+"\"}";
						FLogger.Log(e);
					}
				}
			}
		}
	}
}
