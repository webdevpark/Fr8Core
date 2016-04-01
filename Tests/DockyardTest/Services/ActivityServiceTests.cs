﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Data.Constants;
using Data.Crates;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using StructureMap;
using Data.Entities;
using Data.Infrastructure;
using Data.Infrastructure.StructureMap;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Hub.Interfaces;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Terminal;
using Hub.Services;
using TerminalBase.Infrastructure;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using Action = Hub.Services.Activity;

namespace DockyardTest.Services
{
    [TestFixture]
    [Category("ActivityService")]
    public class ActivityServiceTests : BaseTest
    {
        private IActivity _activity;
        private ICrateManager _crate;
        private bool _eventReceived;
        private Mock<ITerminalTransmitter> TerminalTransmitterMock
        {
            get { return Mock.Get(ObjectFactory.GetInstance<ITerminalTransmitter>()); }
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            TerminalBootstrapper.ConfigureTest();

            _activity = ObjectFactory.GetInstance<IActivity>();
            _crate = ObjectFactory.GetInstance<ICrateManager>();
            _eventReceived = false;

            FixtureData.AddTestActivityTemplate();
        }
        
        [Test]
        [ExpectedException(ExpectedException = typeof(ArgumentNullException))]
        public async Task Activity_Configure_WithNullActionTemplate_ThrowsArgumentNullException()
        {
            var _service = new Activity(ObjectFactory.GetInstance<ICrateManager>(), ObjectFactory.GetInstance<IAuthorization>(), ObjectFactory.GetInstance<ISecurityServices>(), ObjectFactory.GetInstance<IActivityTemplate>(), ObjectFactory.GetInstance<IPlanNode>());

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                await _service.Configure(uow, null, null);
            }
        }

        [Test]
        public async Task CanCRUDActivities()
        {
            ActivityDO origActivityDO;

            using (IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var plan = FixtureData.TestPlan1();
                uow.PlanRepository.Add(plan);

                var subPlane = FixtureData.TestSubPlanDO1();
                plan.ChildNodes.Add(subPlane);

                origActivityDO = new FixtureData(uow).TestActivity3();

                origActivityDO.ParentPlanNodeId = subPlane.Id;

                uow.ActivityTemplateRepository.Add(origActivityDO.ActivityTemplate);
                uow.SaveChanges();
            }

            IActivity activity = new Activity(ObjectFactory.GetInstance<ICrateManager>(), ObjectFactory.GetInstance<IAuthorization>(), ObjectFactory.GetInstance<ISecurityServices>(), ObjectFactory.GetInstance<IActivityTemplate>(), ObjectFactory.GetInstance<IPlanNode>());

            //Add
                await activity.SaveOrUpdateActivity(origActivityDO);
            
            ActivityDO activityDO;
            //Get
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                activityDO = activity.GetById(uow, origActivityDO.Id);
            }

            Assert.AreEqual(origActivityDO.Id, activityDO.Id);
            Assert.AreEqual(origActivityDO.CrateStorage, activityDO.CrateStorage);

            Assert.AreEqual(origActivityDO.Ordering, activityDO.Ordering);

            ISubPlan subPlan = new SubPlan();
            //Delete
            await subPlan.DeleteActivity(null, activityDO.Id, true);
        }

        [Test]
        public async Task ActivityWithNestedUpdated_StructureUnchanged()
        {
            var tree = FixtureData.CreateTestActivityTreeWithOnlyActivityDo();
            var updatedTree = FixtureData.CreateTestActivityTreeWithOnlyActivityDo();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var plan = new PlanDO
                {
                    PlanState = PlanState.Active,
                    Name = "name",
                    ChildNodes = {tree}
                };
                uow.PlanRepository.Add(plan);
                uow.SaveChanges();

                Visit(updatedTree, x => x.Label = string.Format("We were here {0}", x.Id));
            }

            await _activity.SaveOrUpdateActivity(updatedTree);

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            { 
                var result = uow.PlanRepository.GetById<ActivityDO>(tree.Id);
                Compare(updatedTree, result, (r, a) =>
                {
                    if (r.Label != a.Label)
                    {
                        throw new Exception("Update failed");
                    }
                });
            }
        }

        [Test]
        public async Task ActivityWithNestedUpdated_RemoveElements()
        {
            var tree = FixtureData.CreateTestActivityTreeWithOnlyActivityDo();
            var updatedTree = FixtureData.CreateTestActivityTreeWithOnlyActivityDo();


            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var plan = new PlanDO
                {
                    PlanState = PlanState.Active,
                    Name = "name",
                    ChildNodes = {tree}
                };
                uow.PlanRepository.Add(plan);
                uow.SaveChanges();
                int removeCounter = 0;

                Visit(updatedTree, a =>
                {
                    if (removeCounter%3 == 0 && a.ParentPlanNode != null)
                    {
                        a.ParentPlanNode.ChildNodes.Remove(a);
                    }

                    removeCounter++;
                });
            }
            
            await _activity.SaveOrUpdateActivity(updatedTree);

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var result = uow.PlanRepository.GetById<ActivityDO>(tree.Id);
                Compare(updatedTree, result, (r, a) =>
                {
                    if (r.Id != a.Id)
                    {
                        throw new Exception("Update failed");
                    }
                });
            }
        }

        [Test]
        public async Task ActivityWithNestedUpdated_AddElements()
        {
            var tree = FixtureData.CreateTestActivityTreeWithOnlyActivityDo();
            var updatedTree = FixtureData.CreateTestActivityTreeWithOnlyActivityDo();


            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var plan = new PlanDO
                {
                    Name = "name",
                    PlanState = PlanState.Active,
                    ChildNodes = {tree}
                };

                uow.PlanRepository.Add(plan);
                uow.SaveChanges();

                int addCounter = 0;

                Visit(updatedTree, a =>
                {
                    if (addCounter%3 == 0 && a.ParentPlanNode != null)
                    {
                        var newAction = new ActivityDO
                        {
                            Id = FixtureData.GetTestGuidById(addCounter + 666),
                            ParentPlanNode = a,
                            ActivityTemplateId = 1
                        };

                        a.ParentPlanNode.ChildNodes.Add(newAction);
                    }

                    addCounter++;
                });

                for (int i = 0; i < 4; i++)
                {
                    Visit(updatedTree, a =>
                    {
                        // if (a.Id > 666)
                        if (FixtureData.GetTestIdByGuid(a.Id) > 666)
                        {
                            var newAction = new ActivityDO
                            {
                                Id = FixtureData.GetTestGuidById(addCounter + 666),
                                ParentPlanNode = a,
                                ActivityTemplateId = 1
                            };

                            a.ParentPlanNode.ChildNodes.Add(newAction);
                        }

                        addCounter++;
                    });
                }

                updatedTree.ParentPlanNodeId = plan.Id;
            }

            await _activity.SaveOrUpdateActivity( updatedTree);

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var result = uow.PlanRepository.GetById<ActivityDO>(tree.Id);
                Compare(updatedTree, result, (r, a) =>
                {
                    if (r.Id != a.Id)
                    {
                        throw new Exception("Update failed");
                    }
                });
            }
        }

        private void Compare(ActivityDO reference, ActivityDO actual, Action<ActivityDO, ActivityDO> callback)
        {
            callback(reference, actual);

            if (reference.ChildNodes.Count != actual.ChildNodes.Count)
            {
                throw new Exception("Unable to compare nodes with different number of children.");
            }

            for (int i = 0; i < reference.ChildNodes.Count; i++)
            {
                Compare((ActivityDO)reference.ChildNodes[i], (ActivityDO)actual.ChildNodes[i], callback);
            }
        }

        private void Visit(ActivityDO activity, Action<ActivityDO> callback)
        {
            callback(activity);

            foreach (var child in activity.ChildNodes.OfType<ActivityDO>().ToArray())
            {
                Visit(child, callback);
            }
        }
        
        [Test]
        public void Process_ActivityUnstarted_ShouldBeCompleted()
        {
            //Arrange
            ActivityDO activityDo = FixtureData.TestActivityUnstarted();
            activityDo.ActivityTemplate.Terminal.Endpoint = "http://localhost:53234/activities/configure";
            activityDo.CrateStorage = JsonConvert.SerializeObject(new ActivityDTO());

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.ActivityTemplateRepository.Add(activityDo.ActivityTemplate);
                uow.PlanRepository.Add(new PlanDO(){
                    Name="name",
                    PlanState = PlanState.Active,
                    ChildNodes = {activityDo}});
                uow.SaveChanges();
            }

            ActivityDTO activityDto = Mapper.Map<ActivityDTO>(activityDo);
            TerminalTransmitterMock.Setup(rc => rc.PostAsync(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Returns(() => Task.FromResult<string>(JsonConvert.SerializeObject(activityDto)));

            ContainerDO containerDO = FixtureData.TestContainer1();

            //Act
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var task = _activity.Run(uow, activityDo, ActivityExecutionMode.InitialRun, containerDO);
                //Assert
                Assert.That(task.Status, Is.EqualTo(TaskStatus.RanToCompletion));
            }
        }

        [Test]
        public void AddCrate_AddCratesDTO_UpdatesActivityCratesStorage()
        {
            ActivityDO activityDO = FixtureData.TestActivity23();

            using (var crateStorage = _crate.GetUpdatableStorage(activityDO))
            {
                crateStorage.AddRange(FixtureData.CrateStorageDTO());
            }

            Assert.IsNotEmpty(activityDO.CrateStorage);
        }
        
        [Test]
        public async Task PrepareToExecute_WithMockedExecute_WithoutPayload()
        {
            ActivityDO activityDo = FixtureData.TestActivityStateInProcess();
            activityDo.CrateStorage = JsonConvert.SerializeObject(new ActivityDTO());

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.ActivityTemplateRepository.Add(activityDo.ActivityTemplate);
                uow.PlanRepository.Add(new PlanDO()
                {
                    Name="sdfsdf",
                    PlanState = PlanState.Active,
                    ChildNodes = { activityDo }
                });
                uow.SaveChanges();
            }

            Activity _activity = ObjectFactory.GetInstance<Action>();
            ContainerDO containerDO = FixtureData.TestContainer1();
            EventManager.EventActionStarted += EventManager_EventActivityStarted;
            var executeActionMock = new Mock<IActivity>();
            executeActionMock.Setup(s => s.Run(It.IsAny<IUnitOfWork>(), activityDo, It.IsAny<ActivityExecutionMode>(), containerDO)).Returns<Task<PayloadDTO>>(null);

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                await _activity.Run(uow, activityDo, ActivityExecutionMode.InitialRun, containerDO);
            }

            Assert.IsNull(containerDO.CrateStorage);
            Assert.IsTrue(_eventReceived);
           // Assert.AreEqual(actionDo.ActionState, ActionState.Active);
        }

            
        [Test]
        public async Task Run_WithMockedExecute_WithPayload()
        {
            ActivityDO activityDo = FixtureData.TestActivityStateInProcess();
            activityDo.CrateStorage = JsonConvert.SerializeObject(new ActivityDTO() { Label = "Test Action" });

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.ActivityTemplateRepository.Add(activityDo.ActivityTemplate);
                uow.PlanRepository.Add(new PlanDO()
                {
                    Name = "name",
                    PlanState = PlanState.Active,
                    ChildNodes = { activityDo }
                });
                uow.SaveChanges();
            }

            IActivity _activity = ObjectFactory.GetInstance<IActivity>();
            ContainerDO containerDO = FixtureData.TestContainer1();
            EventManager.EventActionStarted += EventManager_EventActivityStarted;
            
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var terminalClientMock = new Mock<ITerminalTransmitter>();
                terminalClientMock.Setup(s => s.CallActivityAsync<PayloadDTO>(It.IsAny<string>(), It.IsAny<Fr8DataDTO>(), It.IsAny<string>()))
                                .Returns(Task.FromResult(new PayloadDTO(containerDO.Id)
                                {
                                    CrateStorage = JsonConvert.DeserializeObject<CrateStorageDTO>(activityDo.CrateStorage)
                                }));
                ObjectFactory.Configure(cfg => cfg.For<ITerminalTransmitter>().Use(terminalClientMock.Object));

                var payload = await _activity.Run(uow, activityDo, ActivityExecutionMode.InitialRun, containerDO);
                Assert.IsNotNull(payload);
            }
            
            Assert.IsTrue(_eventReceived);
        }
        
        [Test]
        public async Task ActivityStarted_EventRaisedSuccessfully()
        {
            ActivityDO activityDo = FixtureData.TestActivityStateInProcess();
            activityDo.CrateStorage = JsonConvert.SerializeObject(new ActivityDTO());

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.ActivityTemplateRepository.Add(activityDo.ActivityTemplate);

                uow.PlanRepository.Add(new PlanDO
                {
                    Name="name",
                    PlanState = PlanState.Active,
                    ChildNodes = { activityDo }
                });
                uow.SaveChanges();
            }


            Activity activity = ObjectFactory.GetInstance<Activity>();
            ContainerDO containerDO = FixtureData.TestContainer1();
            EventManager.EventActionStarted += EventManager_EventActivityStarted;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                await activity.Run(uow, activityDo, ActivityExecutionMode.InitialRun, containerDO);
            }

            Assert.IsTrue(_eventReceived,  "Unable to receive event about activity start");
        }
        

        private void EventManager_EventActivityStarted(ActivityDO activity)
        {
            _eventReceived = true;
        }
    }

}
