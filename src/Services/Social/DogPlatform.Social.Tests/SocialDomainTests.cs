using DogPlatform.Social.Domain;
namespace DogPlatform.Social.Tests;
public sealed class SocialDomainTests
{
 [Fact]public void Friend_request_rejects_self(){var id=Guid.NewGuid();Assert.True(FriendRequest.Create(id,id,DateTime.UtcNow).IsFailure);}
 [Fact]public void Only_addressee_can_accept(){var a=Guid.NewGuid();var b=Guid.NewGuid();var r=FriendRequest.Create(a,b,DateTime.UtcNow).Value;Assert.True(r.Accept(a,DateTime.UtcNow).IsFailure);Assert.True(r.Accept(b,DateTime.UtcNow).IsSuccess);Assert.Equal(FriendRequestStatus.Accepted,r.Status);}
 [Fact]public void Only_requester_can_cancel(){var a=Guid.NewGuid();var b=Guid.NewGuid();var r=FriendRequest.Create(a,b,DateTime.UtcNow).Value;Assert.True(r.Cancel(b,DateTime.UtcNow).IsFailure);Assert.True(r.Cancel(a,DateTime.UtcNow).IsSuccess);}
 [Fact]public void Friendship_order_is_bidirectional(){var a=Guid.NewGuid();var b=Guid.NewGuid();var x=Friendship.Create(a,b,DateTime.UtcNow);var y=Friendship.Create(b,a,DateTime.UtcNow);Assert.Equal(x.User1Id,y.User1Id);Assert.Equal(x.User2Id,y.User2Id);}
 [Fact]public void Message_requires_content_and_limits_length(){Assert.True(Message.Create(Guid.NewGuid(),Guid.NewGuid()," ",DateTime.UtcNow).IsFailure);Assert.True(Message.Create(Guid.NewGuid(),Guid.NewGuid(),new string('x',2001),DateTime.UtcNow).IsFailure);Assert.True(Message.Create(Guid.NewGuid(),Guid.NewGuid(),"hola",DateTime.UtcNow).IsSuccess);}
 [Fact]public void Rejected_request_cannot_later_be_accepted(){var a=Guid.NewGuid();var b=Guid.NewGuid();var r=FriendRequest.Create(a,b,DateTime.UtcNow).Value;Assert.True(r.Reject(b,DateTime.UtcNow).IsSuccess);Assert.True(r.Accept(b,DateTime.UtcNow).IsFailure);}
 [Fact]public void Block_cancels_pending_request(){var a=Guid.NewGuid();var b=Guid.NewGuid();var r=FriendRequest.Create(a,b,DateTime.UtcNow).Value;r.CancelDueToBlock(DateTime.UtcNow);Assert.Equal(FriendRequestStatus.Cancelled,r.Status);}
 [Fact]public void Conversation_is_one_to_one_and_identifies_counterpart(){var a=Guid.NewGuid();var b=Guid.NewGuid();var c=Conversation.Create(a,b,DateTime.UtcNow);Assert.True(c.Includes(a));Assert.True(c.Includes(b));Assert.Equal(b,c.Other(a));Assert.Equal(2,c.Participants.Count);}
 [Fact]public void Mark_read_is_idempotent(){var a=Guid.NewGuid();var b=Guid.NewGuid();var c=Conversation.Create(a,b,DateTime.UtcNow);var p=c.Participants.Single(x=>x.UserId==a);var messageId=Guid.NewGuid();p.MarkRead(messageId,DateTime.UtcNow);p.MarkRead(messageId,DateTime.UtcNow);Assert.Equal(messageId,p.LastReadMessageId);}
 [Fact]public void Message_sender_is_fixed_at_creation(){var sender=Guid.NewGuid();var m=Message.Create(Guid.NewGuid(),sender,"hola",DateTime.UtcNow).Value;Assert.Equal(sender,m.SenderUserId);}
}
