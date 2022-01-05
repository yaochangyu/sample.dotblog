using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void Survey_CreateTable()
    {
        var client = CreateAmazonDynamoDbClient();
        var request = new CreateTableRequest
        {
            AttributeDefinitions = new List<AttributeDefinition>()
            {
                new AttributeDefinition
                {
                    AttributeName = "Id",
                    AttributeType = "N"
                },
                new AttributeDefinition
                {
                    AttributeName = "DateTime",
                    AttributeType = "S"
                }
            },

            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "Id",
                    KeyType = "HASH" //Partition key
                },
                new KeySchemaElement
                {
                    AttributeName = "DateTime",
                    KeyType = "RANGE" //Range key
                }
            }
        };
        var response = client.CreateTableAsync(request).Result;
    }
    
    private static void CreateExampleTable(AmazonDynamoDBClient client, 
                                           string tableName,
                                           CancellationToken cancel)
    {
        Console.WriteLine("\n*** Creating table ***");
        var request = new CreateTableRequest
        {
            AttributeDefinitions = new List<AttributeDefinition>()
            {
                new AttributeDefinition
                {
                    AttributeName = "Id",
                    AttributeType = "N"
                },
                new AttributeDefinition
                {
                    AttributeName = "ReplyDateTime",
                    AttributeType = "N"
                }
            },
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "Id",
                    KeyType = "HASH" //Partition key
                },
                new KeySchemaElement
                {
                    AttributeName = "ReplyDateTime",
                    KeyType = "RANGE" //Sort key
                }
            },
            ProvisionedThroughput = new ProvisionedThroughput
            {
                ReadCapacityUnits = 5,
                WriteCapacityUnits = 6
            },
            TableName = tableName
        };

        var response = client.CreateTableAsync(request, cancel).Result;

        var tableDescription = response.TableDescription;
        Console.WriteLine("{1}: {0} \t ReadsPerSec: {2} \t WritesPerSec: {3}",
            tableDescription.TableStatus,
            tableDescription.TableName,
            tableDescription.ProvisionedThroughput.ReadCapacityUnits,
            tableDescription.ProvisionedThroughput.WriteCapacityUnits);

        string status = tableDescription.TableStatus;
        Console.WriteLine(tableName + " - " + status);

        WaitUntilTableReady(client, tableName,cancel);
    }
    private static void WaitUntilTableReady(AmazonDynamoDBClient client, string tableName,CancellationToken cancel)
    {
        string status = null;
        // Let us wait until table is created. Call DescribeTable.
        do
        {
            System.Threading.Thread.Sleep(5000); // Wait 5 seconds.
            try
            {
                var res = client.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = tableName
                }, cancel).Result;

                Console.WriteLine("Table name: {0}, status: {1}",
                    res.Table.TableName,
                    res.Table.TableStatus);
                status = res.Table.TableStatus;
            }
            catch (ResourceNotFoundException)
            {
                // DescribeTable is eventually consistent. So you might
                // get resource not found. So we handle the potential exception.
            }
        } while (status != "ACTIVE");
    }
    private static AmazonDynamoDBClient? CreateAmazonDynamoDbClient()
    {
        var clientConfig = new AmazonDynamoDBConfig
        {
            ServiceURL = "http://localhost:8000"
        };
        var client = new AmazonDynamoDBClient(clientConfig);
        return client;
    }
}