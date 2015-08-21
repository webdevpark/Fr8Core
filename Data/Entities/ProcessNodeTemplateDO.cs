﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.States;
using Data.States.Templates;
using Data.Validations;

namespace Data.Entities
{
    public class ProcessNodeTemplateDO : BaseDO
    {
        public ProcessNodeTemplateDO()
        {
            this.Criteria = new List<CriteriaDO>();
            this.ActionLists = new List<ActionListDO>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; }

        [ForeignKey("ProcessTemplate")]
        public int? ParentTemplateId { get; set; }

        public virtual ProcessTemplateDO ProcessTemplate { get; set; }

        /// <summary>
        /// this is a JSON structure that is a array of key-value pairs that represent possible transitions. Example:
        ///[{'Flag':'true','Id':'234kljdf'},{'Flag':'false','Id':'dfgkjfg'}]. In this case the values are Id's of other ProcessNodes.
        /// </summary>
        public string NodeTransitions { get; set; }

        public virtual List<CriteriaDO> Criteria { get; set; }

        public virtual List<ActionListDO> ActionLists { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
        public override void BeforeSave()
        {
            base.BeforeSave();

                        
            ProcessNodeTemplatetValidator pntValidator = new ProcessNodeTemplatetValidator();
            FluentValidation.Results.ValidationResult results = pntValidator.Validate(this);
            if (!results.IsValid)
            {
                foreach (var failure in results.Errors)
                {
                    throw new Exception("Property " + failure.PropertyName + " failed validation. Error was: " + failure.ErrorMessage);
                }
            }
        }
    }
}