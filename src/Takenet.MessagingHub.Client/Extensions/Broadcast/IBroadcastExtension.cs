﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Takenet.MessagingHub.Client.Extensions.Broadcast
{
    /// <summary>
    /// Provide a distribution list management service for message broadcasting.
    /// </summary>
    public interface IBroadcastExtension
    {
        /// <summary>
        /// Creates a distribution list with the specified name.
        /// </summary>
        /// <param name="listName">Name of the list.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task CreateDistributionListAsync(string listName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes an existing distribution list.
        /// </summary>
        /// <param name="listName">Name of the list.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task DeleteDistributionListAsync(string listName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Adds a recipient identity to an existing distribution list.
        /// </summary>
        /// <param name="listName">Name of the list.</param>
        /// <param name="recipientIdentity">The recipient identity.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task AddRecipientAsync(string listName, Identity recipientIdentity, CancellationToken cancellationToken = default(CancellationToken));


        /// <summary>
        /// Deletes a recipient identity from an existing distribution list.
        /// </summary>
        /// <param name="listName">Name of the list.</param>
        /// <param name="recipientIdentity">The recipient identity.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task DeleteRecipientAsync(string listName, Identity recipientIdentity, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the list identity from a name.
        /// </summary>
        /// <param name="listName">Name of the list.</param>
        /// <returns></returns>
        Identity GetListIdentity(string listName);

        /// <summary>
        /// Sends a message to a distribution list with the specified content.
        /// </summary>
        /// <param name="listName">Name of the list.</param>
        /// <param name="content">The content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SendMessageAsync(string listName, Document content, CancellationToken cancellationToken = default(CancellationToken));
    }
}
