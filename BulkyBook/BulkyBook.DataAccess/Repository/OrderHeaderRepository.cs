using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository;

public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
{
    public OrderHeaderRepository(ApplicationDbContext db) : base(db)
    {
    }

    public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
    {
	    var order = _db.OrderHeaders.FirstOrDefault(o => o.Id == id);

	    if (order != null)
	    {
		    order.OrderStatus = orderStatus;
		    
		    if(!string.IsNullOrEmpty(paymentStatus)) 
			    order.PaymentStatus = paymentStatus;
	    }
    }

    public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
    {
	    var order = _db.OrderHeaders.FirstOrDefault(o => o.Id == id);

	    if (order != null)
	    {
		    if(!string.IsNullOrEmpty(sessionId))
			    order.SessionId = sessionId;
			
			if(!string.IsNullOrEmpty(paymentIntentId))
			{
				order.PaymentIntentId = paymentIntentId;
				order.PaymentDate = DateTime.Now;
			}
	    }
    }
}