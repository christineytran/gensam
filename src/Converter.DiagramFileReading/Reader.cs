﻿using Converter.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Converter.DiagramFileReading
{
    public class Reader
    {
        private readonly Application application;

        public Reader(Application application)
        {
            this.application = application;
        }

        public void ReadFile(string filePath)
        {
            var root = XElement.Load(filePath) //mxfile
                .Elements("diagram").FirstOrDefault()
                .Elements("mxGraphModel").FirstOrDefault()
                .Elements("root").FirstOrDefault()
                ;
            List<XElement> vertices =
                (from el in root.Elements("mxCell")
                 where (string)el.Attribute("vertex") == "1"
                 select el).ToList();

            List<XElement> edges =
                (from el in root.Elements("mxCell")
                 where (string)el.Attribute("edge") == "1"
                 select el).ToList();

            foreach (XElement el in vertices)
            {
                var c = ReadComponent(el);
                if (c != null)
                {
                    application.RegisterComponent(el.Attribute("id").Value, c);
                }
            }
            foreach (XElement edge in edges)
            {
                var labelElement = 
                (from el in root.Elements("mxCell")
                 where
                    (string)el.Attribute("vertex") == "1"
                    && (string)el.Attribute("parent") == (string)edge.Attribute("id") 
                    && ((string)el.Attribute("style")).Contains("edgeLabel")
                 select el).SingleOrDefault();
                var label = "";
                if (labelElement != null)
                {
                    var html = new HtmlDocument();
                    html.LoadHtml(labelElement.Attribute("value").Value);
                    label = html.DocumentNode.InnerText;
                }
                application.RegisterConnection(edge.Attribute("source").Value, edge.Attribute("target").Value, label);
            }
        }

        Component ReadComponent(XElement element)
        {
            if (element.Attribute("vertex")?.Value != "1")
                return null;
            var html = new HtmlDocument();
            html.LoadHtml(element.Attribute("value").Value);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.lambda_function"))
                return new Component(html.DocumentNode.InnerText,ComponentType.Function);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.client"))
                return new Component("_browser", ComponentType.Browser);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.endpoint"))
                return new Component(html.DocumentNode.InnerText, ComponentType.RestEndpoint);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.table"))
                return new Component(html.DocumentNode.InnerText, ComponentType.Table);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.topic"))
                return new Component(html.DocumentNode.InnerText, ComponentType.Topic);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.queue"))
                return new Component(html.DocumentNode.InnerText, ComponentType.Queue);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.bucket"))
                return new Component(html.DocumentNode.InnerText, ComponentType.Bucket);
            return null;
        }
    }

}
