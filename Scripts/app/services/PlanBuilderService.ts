/// <reference path="../_all.ts" />

/*
    The service enables operations with Process Templates
*/
module dockyard.services {
    export interface IPlanService extends ng.resource.IResourceClass<interfaces.IPlanFullDTO> {
        getbystatus: (id: { id: number; status: number; category?: string }) => Array<interfaces.IPlanVM>;
        getFull: (id: Object) => interfaces.IPlanFullDTO;
        getByActivity: (id: { id: string }) => interfaces.IPlanVM;
        execute: (id: { id: number }, payload: { payload: string }, success: any, error: any) => void;
        activate: (data: { planId: string, planBuilderActivate: boolean }) => any;
        deactivate: (data: { planId: string }) => ng.resource.IResource<string>;
        update: (data: { id: string, name: string }) => interfaces.IPlanVM;
        run: (id: string) => ng.IPromise<model.ContainerDTO>;
        runAndProcessClientAction: (id: string) => ng.IPromise<model.ContainerDTO>;
    }

    export interface ISubPlanService extends ng.resource.IResourceClass<interfaces.ISubPlanVM> {
        create: (subPlan: model.SubPlanDTO) => interfaces.ISubPlanVM;
        update: (subPlan: model.SubPlanDTO) => interfaces.ISubPlanVM;
    }

    export interface IActionService extends ng.resource.IResourceClass<interfaces.IActionVM> {
        configure: (action: interfaces.IActivityDTO) => ng.resource.IResource<interfaces.IActionVM>;
        getByPlan: (id: Object) => ng.resource.IResource<Array<interfaces.IActionVM>>;
        create: (args: { actionTemplateId: number, name: string, label: string, parentNodeId: number, createPlan: boolean }) => ng.resource.IResource<model.PlanDTO | model.ActivityDTO>;
        createSolution: (args: { solutionName: string }) => ng.resource.IResource<model.PlanFullDTO>
        //TODO make resource class do this operation
        deleteById: (id: { id: string; confirmed: boolean }) => ng.resource.IResource<string>
        batchSave: (actionList: interfaces.IActivityDTO[]) => ng.resource.IResource<interfaces.IActionVM>
        //getFieldDataSources: (params: Object, data: interfaces.IActionVM) => interfaces.IDataSourceListVM;
    }

    export interface IDocuSignTemplateService extends ng.resource.IResourceClass<interfaces.IDocuSignTemplateVM> { }

    export interface IDocuSignTriggerService extends ng.resource.IResourceClass<interfaces.IDocuSignExternalEventVM> { }

    interface __ISubPlanService extends ng.resource.IResourceClass<interfaces.ISubPlanVM> {
        add: (curProcessNodeTemplate: model.SubPlanDTO) => interfaces.ISubPlanVM;
        update: (curProcessNodeTemplate: model.SubPlanDTO) => interfaces.ISubPlanVM;
    }

    interface __ICriteriaService extends ng.resource.IResourceClass<interfaces.ICriteriaVM> {
        update: (curCriteria: model.CriteriaDTO) => interfaces.ICriteriaVM;
        byProcessNodeTemplate: (id: { id: string }) => interfaces.ICriteriaVM;
    }

    export interface ICriteriaServiceWrapper {
        load: (id: string) => ng.IPromise<model.SubPlanDTO>;
        add: (curProcessNodeTemplate: model.SubPlanDTO) => ng.IPromise<model.SubPlanDTO>;
        update: (curProcessNodeTemplate: model.SubPlanDTO) => ng.IPromise<model.SubPlanDTO>;
        addOrUpdate(curProcessNodeTemplate: model.SubPlanDTO): {
            actionType: ActionTypeEnum;
            promise: ng.IPromise<model.SubPlanDTO>
        }
    }

    export interface IPlanBuilderService {
        saveCurrent(current: model.PlanBuilderState): ng.IPromise<model.PlanBuilderState>
    }

    export interface IActivityTemplateService extends ng.resource.IResourceClass<interfaces.IActivityTemplateVM> {
        getAvailableActivities: () => ng.resource.IResource<Array<interfaces.IActivityCategoryDTO>>;
    }

    /*
        PlanDTO CRUD service.
    */

    app.factory('PlanService', [
        '$resource',
        '$http',
        '$q',
        '$location',
        'ngToast',
        function (
            $resource: ng.resource.IResourceService,
            $http: ng.IHttpService,
            $q: ng.IQService,
            $location: ng.ILocationService,
            ngToast: any
        ): IPlanService {

            var resource = <IPlanService>$resource(
                '/api/plans?id=:id',
                { id: '@id' },
                {
                    'save': {
                        method: 'POST',
                        url: '/api/plans/post'
                    },
                    'getFull': {
                        method: 'GET',
                        isArray: false,
                        url: '/api/plans/full/:id',
                        params: {
                            id: '@id'
                        }
                    },
                    'getbystatus': {
                        method: 'GET',
                        isArray: true,
                        url: '/api/plans/status?status=:status&category=:category',
                        params: {
                            status: '@status',
                            category: '@category'
                        }
                    },
                    'getByActivity': {
                        method: 'GET',
                        isArray: false,
                        url: '/api/plans/getByActivity/:id',
                        params: {
                            id: '@id'
                        }
                    },
                    'execute': {
                        method: 'POST',
                        isArray: false,
                        url: '/api/plans/run?planId=:id',
                        params: {
                            id: '@id'
                        }
                    },
                    'activate': {
                        method: 'POST',
                        isArray: false,
                        url: '/api/plans/activate/',
                        params: {
                            planId: '@planId',
                            planBuilderActivate: '@planBuilderActivate'
                        }
                    },
                    'deactivate': {
                        method: 'POST',
                        isArray: false,
                        url: '/api/plans/deactivate/',
                        params: {
                            planId: '@planId'
                        }
                    },
                    'update': {
                        method: 'POST',
                        url: '/api/plans/',
                        params: {

                        }
                    }
                });

            resource.run = (id: string): ng.IPromise<model.ContainerDTO> => {
                var url = '/api/plans/run?planId=' + id;

                var d = $q.defer();

                $http.post(url, null)
                    .then((res: any) => {
                        d.resolve(res.data);
                    })
                    .catch((err: any) => {
                        d.reject(err);
                    });

                return d.promise;
            };

            resource.runAndProcessClientAction =
                (id: string): ng.IPromise<model.ContainerDTO> => {
                    var d = $q.defer();

                    resource.run(id)
                        .then((container: model.ContainerDTO) => {
                            if (container
                                && container.currentActivityResponse == model.ActivityResponse.ExecuteClientAction
                                && container.currentClientActivityName) {

                                switch (container.currentClientActivityName) {
                                    case 'ShowTableReport':
                                        var path = '/findObjects/' + container.id + '/results';
                                        $location.path(path);
                                        break;

                                    default:
                                        break;
                                }
                            }

                            if (container && container.error != null) {
                                var messageToShow = "Plan " + container.name + " failed." + "<br/>";
                                messageToShow += "Action: " + container.error.currentActivity + "<br/>";
                                messageToShow += "Terminal: " + container.error.currentTerminal + "<br/>";
                                messageToShow += "Message: " + container.error.message;
                                ngToast.danger(messageToShow);
                            }

                            d.resolve(container);
                        })
                        .catch((err: any) => {
                            d.reject(err);
                        });

                    return d.promise;
                };

            return resource;
        }
    ]);

    /*
        DocuSignTemplateDTO CRUD service.
    */
    app.factory('DocuSignTemplateService', ['$resource', ($resource: ng.resource.IResourceService): IDocuSignTemplateService =>
        <IDocuSignTemplateService>$resource('/api/docusigntemplate')
    ]);

    /* 
        DocuSignExternalEventDTO CRUD service.
    */
    app.factory('DocuSignTriggerService', ['$resource', ($resource: ng.resource.IResourceService): IDocuSignTriggerService =>
        <IDocuSignTriggerService>$resource('/api/plan/triggersettings')
    ]);

    
    app.factory('SubPlanService', ['$resource', ($resource: ng.resource.IResourceService): ISubPlanService =>
        <ISubPlanService>$resource('/api/subplans/', null,
            {
                'create': {
                    method: 'POST',
                    isArray: false,
                    url: '/api/subplans'
                },
                'update': {
                    method: 'PUT',
                    isArray: false,
                    url: '/api/subplans'
                }
            })
    ]);

    app.factory('ActionTemplateService', ['$resource', ($resource: ng.resource.IResourceService): IActionService =>
        <IActionService>$resource('/api/actiontemplates/', null,
            {
                'available': {
                    method: 'GET',
                    isArray: true,
                    url: '/api/plannodes/available',
                    params: {
                        tag: '@tag'
                    }
                }
            })
    ]);

    /* 
        ActivityDTO CRUD service.
    */
    app.factory('ActionService', ['$resource', ($resource: ng.resource.IResourceService): IActionService =>
        <IActionService>$resource('/api/activities?id=:id',
            {
                id: '@id'
            },
            {
                'save': {
                    method: 'POST',
                    isArray: false,
                    url: '/api/activities/save',
                    params: {
                        suppressSpinner: true // Do not show page-level spinner since we have one within the Configure Action pane
                    }
                },
                //'get': {
                //    transformResponse: function (data) {
                //        //Map a proto-action object to an actual ActionDesignDTO instance so that we can access methods 
                //        var dataObject: interfaces.IActionDesignDTO = angular.fromJson(data);
                //        return model.ActionDesignDTO.create(dataObject);
                //    }
                //},
                'delete': { method: 'DELETE' },
                'configure': {
                    method: 'POST',
                    url: '/api/activities/configure',
                    params: {
                        suppressSpinner: true // Do not show page-level spinner since we have one within the Configure Action pane
                    }
                },
                'getByPlan': {
                    method: 'GET',
                    url: '/api/activities/bypt',
                    isArray: true
                },
                'deleteById': {
                    method: 'DELETE',
                    url: '/api/activities?id=:id&confirmed=:confirmed'
                },
                'create': {
                    method: 'POST',
                    url: '/api/activities/create'
                },
                'createSolution': {
                    method: 'POST',
                    url: '/api/activities/create',
                    params: {
                        solutionName: '@solutionName'
                    }
                },
                'params': {
                    id: 'id'
                }
            })
    ]);

    /* 
        CriteriaDTO CRUD service.
        This service is not intended to be used by anything except CriteriaServiceWrapper,
        that's why its name starts with underscores. 
    */
    app.factory('__CriteriaService', ['$resource', ($resource: ng.resource.IResourceService): __ICriteriaService =>
        <__ICriteriaService>$resource('/api/criteria', null,
            {
                'update': {
                    method: 'PUT'
                },
                'save': {
                    method: 'PUT'
                },
                'byProcessNodeTemplate': {
                    method: 'GET',
                    url: '/api/criteria/byProcessNodeTemplate'
                }
            })
    ]);

    /* 
        ProcessNodeTemplateDTO CRUD service.
        This service is not intended to be used by anything except CriteriaServiceWrapper,
        that's why its name starts with underscores. 
    */
    app.factory('__SubPlanService', ['$resource', ($resource: ng.resource.IResourceService): __ISubPlanService =>
        <__ISubPlanService>$resource('/api/processnodetemplate', null,
            {
                'add': {
                    method: 'POST'
                },
                'update': {
                    method: 'PUT'
                }
            })
    ]);

    app.factory('ActivityTemplateService', ['$resource', ($resource: ng.resource.IResourceService): IActivityTemplateService =>
        <IActivityTemplateService>$resource('/api/activityTemplates/:id', { id: '@id' },
            {
                'getAvailableActivities': {
                    method: 'GET',
                    url: '/api/plannodes/available/',
                    isArray: true
                }
            })
    ]);

    /*
        General data persistance methods for PlanBuilder.
    */
    class PlanBuilderService implements IPlanBuilderService {
        constructor(
            private $q: ng.IQService,
            private CriteriaServiceWrapper: ICriteriaServiceWrapper,
            private ActionService: IActionService,
            private crateHelper: CrateHelper
        ) { }

        public saveCurrent(currentState: model.PlanBuilderState): ng.IPromise<model.PlanBuilderState> {
            var deferred = this.$q.defer<model.PlanBuilderState>(),
                newState = new model.PlanBuilderState()

            // TODO: bypass save for unchanged entities
              
            // Save processNodeTemplate if not null
            if (currentState.subPlan) {
                this.CriteriaServiceWrapper.addOrUpdate(currentState.subPlan).promise
                    .then((result: interfaces.ISubPlanVM) => {
                        //new model.CriteriaDTO(result.criteria.id, false, result.criteria.id, model.CriteriaExecutionType.NoSet);
                        newState.subPlan = result;

                        this.crateHelper.mergeControlListCrate(
                            currentState.activities.configurationControls,
                            currentState.activities.crateStorage
                        );

                        // If an Action is selected, save it
                        if (currentState.activities) {
                            return this.ActionService.save({ id: currentState.activities.id },
                                currentState.activities, null, null);
                        }
                        else {
                            return deferred.resolve(newState);
                        }
                    })
                    .then((result: interfaces.IActionVM) => {
                        newState.activities = result;
                        return deferred.resolve(newState);
                    })
                    .catch((reason: any) => {
                        return deferred.reject(reason);
                    });
            }

            //Save Action only
            else if (currentState.activities) {
                this.crateHelper.mergeControlListCrate(
                    currentState.activities.configurationControls,
                    currentState.activities.crateStorage
                );

                var promise = this.ActionService.save(
                    { id: currentState.activities.id },
                    currentState.activities,
                    null,
                    null).$promise;
                promise
                    .then((result: interfaces.IActionVM) => {
                        newState.activities = result;
                        return deferred.resolve(newState);
                    })
                    .catch((reason: any) => {
                        return deferred.reject(reason);
                    });
            }
            else {
                //Nothing to save
                deferred.resolve(newState);
            }
            return deferred.promise;
        }
    }

    /*
        Register PlanBuilderService with AngularJS
    */
    app.factory('PlanBuilderService', ['$q', 'CriteriaServiceWrapper', 'ActionService', 'CrateHelper', 'UIHelperService', (
        $q: ng.IQService,
        CriteriaServiceWrapper: ICriteriaServiceWrapper,
        ActionService: IActionService,
        crateHelper: CrateHelper) => {
        return new PlanBuilderService($q, CriteriaServiceWrapper, ActionService, crateHelper);
    }
    ]);

    /*
        A Service which is trying to encapsulate the fact that working with a Criteria in UI 
        involves updating two entities: CriteriaDTO and ProcessNodeTemplateDTO. 
        The name is not good but I could not come up with a better one. 
        If we combined CriteriaDTO and ProcessNodeTemplateDTO, this complexity would not be necessary.
    */
    class CriteriaServiceWrapper implements ICriteriaServiceWrapper {
        public constructor(
            private CriteriaService: __ICriteriaService,
            private SubPlanService: __ISubPlanService,
            private $q: ng.IQService) {
        }

        public add(curProcessNodeTemplate: model.SubPlanDTO): ng.IPromise<model.SubPlanDTO> {
            var deferred = this.$q.defer<interfaces.ISubPlanVM>();

            // Save ProcessNodeTemplate object to server.
            // Server automatically creates empty criteria node.
            var addDeferred = this.SubPlanService.add(curProcessNodeTemplate);

            addDeferred.$promise
                .then((addResult: interfaces.ISubPlanVM) => {
                    curProcessNodeTemplate.isTempId = false;
                    curProcessNodeTemplate.subPlanId = addResult.subPlanId;    
                    // Fetch criteria object from server by ProcessNodeTemplate global ID.
                    return this.CriteriaService.byProcessNodeTemplate({ id: addResult.subPlanId }).$promise;
                })
                .then((getResult: interfaces.ICriteriaVM) => {
                    curProcessNodeTemplate.criteria.id = getResult.id;
                    curProcessNodeTemplate.criteria.isTempId = false;
                    // Update criteria object on server.
                    return this.CriteriaService.save({ id: getResult.id }).$promise;
                })
                .then((updateResult: interfaces.ICriteriaVM) => {
                    deferred.resolve(addDeferred);
                })
                .catch((reason: any) => {
                    deferred.reject(reason);
                });

            return deferred.promise;
        }

        public update(curProcessNodeTemplate: model.SubPlanDTO): ng.IPromise<model.SubPlanDTO> {
            var deferred = this.$q.defer<interfaces.ISubPlanVM>();

            // Save ProcessNodeTemplate object to server.
            // Server automatically creates empty criteria node.
            var updateDeferred = this.SubPlanService.update(curProcessNodeTemplate);

            updateDeferred.$promise
                .then((updateResult: interfaces.ISubPlanVM) => {   
                    // Call REST api to update Criteria entity on server.
                    return this.CriteriaService.update(curProcessNodeTemplate.criteria).$promise;
                })
                .then((updateResult: interfaces.ICriteriaVM) => {
                    deferred.resolve(updateDeferred);
                })
                .catch((reason: any) => {
                    deferred.reject(reason);
                });

            return deferred.promise;
        }

        public load(id: string): ng.IPromise<model.SubPlanDTO> {
            var deferred = this.$q.defer<interfaces.ISubPlanVM>();

            // Save ProcessNodeTemplate object to server.
            // Server automatically creates empty criteria node.
            var getPntDeferred = this.SubPlanService.get({ id: id }),
                getCriteriaDeferred = this.CriteriaService.byProcessNodeTemplate({ id: id });

            this.$q.all([getPntDeferred.$promise, getCriteriaDeferred.$promise])
                .then(() => {
                    var criteria = new model.CriteriaDTO(
                        getCriteriaDeferred.id,
                        false,
                        getPntDeferred.subPlanId,
                        getCriteriaDeferred.executionType
                    );

                    angular.forEach(getCriteriaDeferred.conditions, function (it: model.Condition) {
                        criteria.conditions.push(it);
                    });

                    getPntDeferred.criteria = criteria;
                    deferred.resolve(getPntDeferred);
                });

            return deferred.promise;
        }

        /*
            This method does adding or updating depending on whether 
            ProcessNodeTemplate has been saved or not.
        */
        public addOrUpdate(curProcessNodeTemplate: model.SubPlanDTO): {
            actionType: ActionTypeEnum,
            promise: ng.IPromise<model.SubPlanDTO>
        } {
            // Don't save anything if there is no criteria selected, 
            // just return a null-valued resolved promise
            if (!curProcessNodeTemplate) {
                var deferred = this.$q.defer<model.SubPlanDTO>();
                deferred.resolve(null);
                return {
                    actionType: ActionTypeEnum.None,
                    promise: deferred.promise
                };
            }

            if (curProcessNodeTemplate.isTempId) {
                // In case of newly created object (i.e. isTempId === true).
                return {
                    actionType: ActionTypeEnum.Add,
                    promise: this.add(curProcessNodeTemplate)
                };
            }
            else {
                return {
                    actionType: ActionTypeEnum.Update,
                    promise: this.update(curProcessNodeTemplate)
                };
            }
        }

    }

    export enum ActionTypeEnum {
        None = 0,
        Add = 1,
        Update = 2
    }

    /*
        Register CriteriaServiceWrapper with AngularJS.
    */
    app.factory('CriteriaServiceWrapper', ['__CriteriaService', '__SubPlanService', '$q',
        (CriteriaService, SubPlanService, $q) => {
            return new CriteriaServiceWrapper(CriteriaService, SubPlanService, $q)
        }]);


}