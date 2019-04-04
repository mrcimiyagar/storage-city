using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using SharedArea.Commands.Internal.Notifications;
using SharedArea.Commands.Internal.Requests;
using SharedArea.Commands.Internal.Responses;
using SharedArea.Entities;
using SharedArea.Middles;
using SharedArea.Utils;
using Version = SharedArea.Entities.Version;

namespace MessengerService
{
    public class NotifConsumer : GlobalConsumer<DatabaseContext>, IConsumer<AccountCreatedWithBackRequest>
        , IConsumer<RoomCreatedWithBackRequest>, IConsumer<ContactCreatedWithBackRequest>, IConsumer<ComplexCreatedWithBackRequest>
    {
        public NotifConsumer(Func<IBusControl> busFetcher) : base(busFetcher, "MessengerService")
        {
            
        }
        
        public async Task Consume(ConsumeContext<ContactCreatedWithBackRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var me = (User) dbContext.BaseUsers.Find(context.Message.Packet.Users[0].BaseUserId);
                var peer = (User) dbContext.BaseUsers.Find(context.Message.Packet.Users[1].BaseUserId);

                var complex = context.Message.Packet.Complex;
                var complexSecret = context.Message.Packet.ComplexSecret;
                var room = context.Message.Packet.Room;
                var m1 = context.Message.Packet.Memberships[0];
                var m2 = context.Message.Packet.Memberships[1];
                var message = context.Message.Packet.ServiceMessage;

                var lComplex = new Complex()
                {
                    ComplexId = complex.ComplexId,
                    Title = complex.Title,
                    Avatar = complex.Avatar,
                    Mode = complex.Mode,
                    ComplexSecret = new ComplexSecret()
                    {
                        ComplexSecretId = complexSecret.ComplexSecretId,
                        Admin = null
                    },
                    Rooms = new List<Room>()
                    {
                        new Room()
                        {
                            RoomId = room.RoomId,
                            Title = room.Title,
                            Avatar = room.Avatar
                        }
                    },
                    Members = new List<Membership>()
                    {
                        new Membership()
                        {
                            MembershipId = m1.MembershipId,
                            User = me,
                            MemberAccess = new MemberAccess()
                            {
                                MemberAccessId = m1.MemberAccess.MemberAccessId,
                                CanCreateMessage = m1.MemberAccess.CanCreateMessage,
                                CanModifyAccess = m1.MemberAccess.CanModifyAccess,
                                CanModifyWorkers = m1.MemberAccess.CanModifyWorkers,
                                CanSendInvite = m1.MemberAccess.CanSendInvite,
                                CanUpdateProfiles = m1.MemberAccess.CanUpdateProfiles
                            }
                        },
                        new Membership()
                        {
                            MembershipId = m2.MembershipId,
                            User = peer,
                            MemberAccess = new MemberAccess()
                            {
                                MemberAccessId = m2.MemberAccess.MemberAccessId,
                                CanCreateMessage = m2.MemberAccess.CanCreateMessage,
                                CanModifyAccess = m2.MemberAccess.CanModifyAccess,
                                CanModifyWorkers = m2.MemberAccess.CanModifyWorkers,
                                CanSendInvite = m2.MemberAccess.CanSendInvite,
                                CanUpdateProfiles = m2.MemberAccess.CanUpdateProfiles
                            }
                        }
                    }
                };

                lComplex.Members[0].MemberAccess.Membership = lComplex.Members[0];
                lComplex.Members[1].MemberAccess.Membership = lComplex.Members[1];

                dbContext.AddRange(lComplex);
                dbContext.SaveChanges();

                var myContact = context.Message.Packet.Contacts[0];
                myContact.Complex = lComplex;
                myContact.User = me;
                myContact.Peer = peer;
                dbContext.Contacts.Add(myContact);

                var peerContact = context.Message.Packet.Contacts[1];
                peerContact.Complex = lComplex;
                peerContact.User = peer;
                peerContact.Peer = me;
                dbContext.Contacts.Add(peerContact);

                dbContext.Messages.Add(message);

                dbContext.SaveChanges();

                SharedArea.Transport.NotifyPacketRouter<EntitiesVersionUpdatedNotif>(
                    Program.Bus,
                    new Packet()
                    {
                        Versions = new List<Version>()
                        {
                            new Version()
                            {
                                VersionId = "Complex_" + complex.ComplexId + "_MessengerService",
                                Number = complex.Version
                            },
                            new Version()
                            {
                                VersionId = "ComplexSecret_" + complexSecret.ComplexSecretId + "_MessengerService",
                                Number = complexSecret.Version
                            },
                            new Version()
                            {
                                VersionId = "Room_" + room.RoomId + "_MessengerService",
                                Number = room.Version
                            },
                            new Version()
                            {
                                VersionId = "Membership_" + m1.MembershipId + "_MessengerService",
                                Number = m1.Version
                            },
                            new Version()
                            {
                                VersionId = "Membership_" + m2.MembershipId + "_MessengerService",
                                Number = m2.Version
                            },
                            new Version()
                            {
                                VersionId = "MemberAccess_" + m1.MemberAccess.MemberAccessId + "_MessengerService",
                                Number = m1.MemberAccess.Version
                            },
                            new Version()
                            {
                                VersionId = "MemberAccess_" + m2.MemberAccess.MemberAccessId + "_MessengerService",
                                Number = m2.MemberAccess.Version
                            },
                            new Version()
                            {
                                VersionId = "Contact_" + myContact.ContactId + "_MessengerService",
                                Number = myContact.Version
                            },
                            new Version()
                            {
                                VersionId = "Contact_" + peerContact.ContactId + "_MessengerService",
                                Number = peerContact.Version
                            }
                        }
                    });

                message = (ServiceMessage) dbContext.Messages.Find(message.MessageId);

                await context.RespondAsync(new ContactCreatedWithBackResponse()
                {
                    Packet = new Packet()
                    {
                        ServiceMessage = message
                    }
                });
            }
        }

        public async Task Consume(ConsumeContext<AccountCreatedWithBackRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var user = context.Message.Packet.User;
                var userSecret = context.Message.Packet.UserSecret;
                var complexSecret = context.Message.Packet.ComplexSecret;
                var message = context.Message.Packet.ServiceMessage;

                user.UserSecret = userSecret;
                userSecret.User = user;
                user.Memberships[0].User = user;
                user.Memberships[0].Complex.ComplexSecret = complexSecret;
                user.Memberships[0].Complex.ComplexSecret.Complex = user.Memberships[0].Complex;
                user.Memberships[0].Complex.ComplexSecret.Admin = user;
                user.Memberships[0].Complex.Rooms[0].Complex = user.Memberships[0].Complex;
                user.UserSecret.Home = user.Memberships[0].Complex;
                user.Memberships[0].User = user;
                user.Memberships[0].MemberAccess.Membership = user.Memberships[0];

                dbContext.AddRange(user);

                dbContext.Messages.Add(message);

                dbContext.SaveChanges();

                SharedArea.Transport.NotifyPacketRouter<EntitiesVersionUpdatedNotif>(
                    Program.Bus,
                    new Packet()
                    {
                        Versions = new List<Version>()
                        {
                            new Version()
                            {
                                VersionId = "BaseUser_" + user.BaseUserId + "_MessengerService",
                                Number = user.Version
                            },
                            new Version()
                            {
                                VersionId = "UserSecret_" + user.UserSecret.UserSecretId + "_MessengerService",
                                Number = user.UserSecret.Version
                            },
                            new Version()
                            {
                                VersionId = "Membership_" + user.Memberships[0].MembershipId + "_MessengerService",
                                Number = user.Memberships[0].Version
                            },
                            new Version()
                            {
                                VersionId = "MemberAccess_" + user.Memberships[0].MemberAccess.MemberAccessId +
                                            "_MessengerService",
                                Number = user.Memberships[0].MemberAccess.Version
                            },
                            new Version()
                            {
                                VersionId = "Complex_" + user.Memberships[0].Complex.ComplexId + "_MessengerService",
                                Number = user.Memberships[0].Complex.Version
                            },
                            new Version()
                            {
                                VersionId = "ComplexSecret_" + user.Memberships[0].Complex.ComplexSecret.ComplexId +
                                            "_MessengerService",
                                Number = user.Memberships[0].Complex.ComplexSecret.Version
                            },
                            new Version()
                            {
                                VersionId = "Room_" + user.Memberships[0].Complex.Rooms[0].RoomId + "_MessengerService",
                                Number = user.Memberships[0].Complex.Rooms[0].Version
                            }
                        }
                    });

                message = (ServiceMessage) dbContext.Messages.Find(message.MessageId);

                await context.RespondAsync(new AccountCreatedWithBackResponse()
                {
                    Packet = new Packet() {ServiceMessage = message}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<RoomCreatedWithBackRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var room = context.Message.Packet.Room;
                var message = context.Message.Packet.ServiceMessage;
                var complex = dbContext.Complexes.Find(room.ComplexId);

                room.Complex = complex;

                dbContext.Rooms.Add(room);

                dbContext.Messages.Add(message);

                dbContext.SaveChanges();
                
                SharedArea.Transport.NotifyPacketRouter<EntitiesVersionUpdatedNotif>(
                    Program.Bus,
                    new Packet()
                    {
                        Versions = new List<Version>()
                        {
                            new Version()
                            {
                                VersionId = "Room_" + room.RoomId + "_MessengerService",
                                Number = room.Version
                            }
                        }
                    });

                message = (ServiceMessage) dbContext.Messages.Find(message.MessageId);

                await context.RespondAsync(new RoomCreatedWithBackResponse()
                {
                    Packet = new Packet() {ServiceMessage = message}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<ComplexCreatedWithBackRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var admin = (User) dbContext.BaseUsers.Find(context.Message.Packet.User.BaseUserId);
                var complex = context.Message.Packet.Complex;
                var complexSecret = context.Message.Packet.ComplexSecret;
                var message = context.Message.Packet.ServiceMessage;
                
                complex.ComplexSecret = complexSecret;
                complexSecret.Complex = complex;
                complex.Rooms[0].Complex = complex;
                complex.Members[0].Complex = complex;
                complexSecret.Admin = admin;
                complex.Members[0].User = admin;
                complex.Members[0].MemberAccess.Membership = complex.Members[0];

                dbContext.AddRange(complex);

                dbContext.Messages.Add(message);

                dbContext.SaveChanges();

                SharedArea.Transport.NotifyPacketRouter<EntitiesVersionUpdatedNotif>(
                    Program.Bus,
                    new Packet()
                    {
                        Versions = new List<Version>()
                        {
                            new Version()
                            {
                                VersionId = "Complex_" + complex.ComplexId + "_MessengerService",
                                Number = complex.Version
                            },
                            new Version()
                            {
                                VersionId = "ComplexSecret_" + complex.ComplexSecret.ComplexSecretId + "_MessengerService",
                                Number = complex.ComplexSecret.Version
                            },
                            new Version()
                            {
                                VersionId = "Room_" + complex.Rooms[0].RoomId + "_MessengerService",
                                Number = complex.Rooms[0].Version
                            },
                            new Version()
                            {
                                VersionId = "Membership_" + complex.Members[0].MembershipId + "_MessengerService",
                                Number = complex.Members[0].Version
                            },
                            new Version()
                            {
                                VersionId = "MemberAccess_" + complex.Members[0].MemberAccess.MemberAccessId + "_MessengerService",
                                Number = complex.Members[0].MemberAccess.Version
                            }
                        }
                    });
                
                await context.RespondAsync(new ComplexCreatedWithBackResponse()
                {
                    Packet = new Packet()
                    {
                        ServiceMessage = message
                    }
                });
            }
        }
    }
}