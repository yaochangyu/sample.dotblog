﻿namespace Lab.MassTransit.WebAPI.Order;

// 訂單已建立
public class OrderCreated
{
    public Guid OrderId { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TotalAmount { get; set; }
}