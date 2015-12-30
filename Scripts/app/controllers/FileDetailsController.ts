﻿/// <reference path="../_all.ts" />

module dockyard.controllers {
    'use strict';

    export interface IFileDetailsScope extends ng.IScope {
        file: model.FileDTO;
    }

    class FileDetailsController {
        // $inject annotation.
        // It provides $injector with information about dependencies to be injected into constructor
        // it is better to have it close to the constructor, because the parameters must match in count and type.
        // See http://docs.angularjs.org/guide/di
        public static $inject = [
            '$scope',
            'ManageFileService',
            '$state'
        ];

        constructor(
            private $scope: IFileDetailsScope,
            private ManageFileService: services.IManageFileService,
            private $state: ng.ui.IState) {

            alert("consstructor")
            ManageFileService.get({ id: $state.params.id }).$promise.then(function (data) {
                alert("2");
                $scope.file = data;
            });
        }
    }

    app.controller('FileDetailsController', FileDetailsController);
}