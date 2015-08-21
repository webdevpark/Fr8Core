﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Data.Entities;

namespace Data.Interfaces.DataTransferObjects
{
    public class ProcessTemplateDTO
    {
        // public ProcessTemplateDTO()
        // {
        //     SubscribedDocuSignTemplates = new List<DocuSignTemplateSubscriptionDO>();
        //     SubscribedExternalEvents = new List<ExternalEventSubscriptionDO>();
        //     DockyardAccount = new DockyardAccountDO();
        // }

        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }
        public int ProcessTemplateState { get; set; }

        // Commented out by yakov.gnusin:
        // Do we really need to provider DockyardAccountDO inside ProcessTemplateDTO?
        // We do override DockyardAccountDO in ProcessTemplateController.Post action.

        // public virtual DockyardAccountDO DockyardAccount { get; set; }
        // public virtual IList<DocuSignTemplateSubscriptionDO> SubscribedDocuSignTemplates { get; set; }
        // public virtual IList<ExternalEventSubscriptionDO> SubscribedExternalEvents { get; set; }
    }
}