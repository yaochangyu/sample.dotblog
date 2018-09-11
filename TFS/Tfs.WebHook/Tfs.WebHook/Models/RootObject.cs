using System;
using System.Collections.Generic;

namespace Tfs.WebHook
{
    public class Account
    {
        public string id { get; set; }
    }

    public class Collection
    {
        public string id { get; set; }
    }

    public class Definition
    {
        public int batchSize { get; set; }

        public string definitionType { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public string triggerType { get; set; }
        public string url { get; set; }
    }

    public class DetailedMessage
    {
        public string html { get; set; }
        public string markdown { get; set; }
        public string text { get; set; }
    }

    public class Drop
    {
        public string downloadUrl { get; set; }
        public string location { get; set; }

        public string type { get; set; }

        public string url { get; set; }
    }

    public class LastChangedBy
    {
        public string displayName { get; set; }

        public string id { get; set; }
        public string imageUrl { get; set; }
        public string uniqueName { get; set; }
        public string url { get; set; }
    }

    public class Log
    {
        public string downloadUrl { get; set; }
        public string type { get; set; }

        public string url { get; set; }
    }

    public class Message
    {
        public string html { get; set; }
        public string markdown { get; set; }
        public string text { get; set; }
    }
    public class Project
    {
        public string id { get; set; }
    }

    public class Queue
    {
        public int id { get; set; }
        public string name { get; set; }
        public string queueType { get; set; }
        public string url { get; set; }
    }

    public class Request
    {
        public int id { get; set; }

        public RequestedFor requestedFor { get; set; }
        public string url { get; set; }
    }

    public class RequestedFor
    {
        public string displayName { get; set; }

        public string id { get; set; }
        public string imageUrl { get; set; }
        public string uniqueName { get; set; }
        public string url { get; set; }
    }
    public class Resource
    {
        public string buildNumber { get; set; }
        public Definition definition { get; set; }
        public Drop drop { get; set; }
        public string dropLocation { get; set; }
        public DateTime finishTime { get; set; }
        public bool hasDiagnostics { get; set; }
        public int id { get; set; }
        public LastChangedBy lastChangedBy { get; set; }
        public Log log { get; set; }
        public Queue queue { get; set; }
        public string reason { get; set; }
        public List<Request> requests { get; set; }
        public bool retainIndefinitely { get; set; }
        public string sourceGetVersion { get; set; }
        public DateTime startTime { get; set; }
        public string status { get; set; }
        public string uri { get; set; }
        public string url { get; set; }
    }
    public class ResourceContainers
    {
        public Account account { get; set; }
        public Collection collection { get; set; }
        public Project project { get; set; }
    }

    public class RootObject
    {
        public DateTime createdDate { get; set; }
        public DetailedMessage detailedMessage { get; set; }
        public string eventType { get; set; }
        public string id { get; set; }
        public Message message { get; set; }
        public int notificationId { get; set; }
        public string publisherId { get; set; }
        public Resource resource { get; set; }
        public ResourceContainers resourceContainers { get; set; }
        public string resourceVersion { get; set; }
        public string subscriptionId { get; set; }
    }
}