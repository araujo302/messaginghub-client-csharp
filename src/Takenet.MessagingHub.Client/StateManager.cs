﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Takenet.MessagingHub.Client
{
    /// <summary>
    /// Provides the management of states for filtering message and notification receivers registered in the application.
    /// </summary>
    internal sealed class StateManager : IStateManager
    {
        public const string DEFAULT_STATE = "default";
        
        private static readonly object SyncRoot = new object();
        private static StateManager _instance;
        private readonly ObjectCache _nodeStateCache;

        private StateManager()
        {
            _nodeStateCache = new MemoryCache(nameof(StateManager));
            StateTimeout = TimeSpan.FromMinutes(30);
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static StateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new StateManager();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Gets or sets the state expiration timeout.
        /// </summary>
        /// <value>
        /// The state timeout.
        /// </value>
        public TimeSpan StateTimeout { get; set; }

        /// <summary>
        /// Gets the last known identity state.
        /// </summary>
        /// <param name="identity">The node.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public string GetState(Identity identity)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            return _nodeStateCache.Get(GetCacheKey(identity)) as string ?? DEFAULT_STATE;
        }

        /// <summary>
        /// Sets the identity state.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <param name="state">The state.</param>
        public void SetState(Identity identity, string state)
        {
            SetState(identity, state, true);
        }

        /// <summary>
        /// Resets the identity state to the default value.
        /// </summary>
        /// <param name="identity">The node.</param>
        public void ResetState(Identity identity)
        {
            SetState(identity, DEFAULT_STATE);
        }

        /// <summary>
        /// Occurs when a identity state is changed.
        /// This event should be used to synchronize multiple application instances states.
        /// </summary>
        public event EventHandler<StateEventArgs> StateChanged;

        internal void SetState(Identity identity, string state, bool raiseEvent)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.Equals(DEFAULT_STATE, StringComparison.OrdinalIgnoreCase))
            {
                _nodeStateCache.Remove(GetCacheKey(identity));
            }
            else
            {
                _nodeStateCache.Set(GetCacheKey(identity), state, new CacheItemPolicy()
                {
                    SlidingExpiration = StateTimeout
                });                
            }

            if (raiseEvent)
            {                
                StateChanged?.Invoke(this, new StateEventArgs(identity, state));
            }
        }

        private static string GetCacheKey(Identity identity) => identity.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Represents an event for the user state.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class StateEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateEventArgs"/> class.
        /// </summary>
        /// <param name="identity">The node.</param>
        /// <param name="state">The state.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public StateEventArgs(Identity identity, string state)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            if (state == null) throw new ArgumentNullException(nameof(state));
            Identity = identity;
            State = state;
        }

        /// <summary>
        /// Gets the identity.
        /// </summary>
        /// <value>
        /// The node.
        /// </value>
        public Identity Identity { get; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public string State { get; }
    }
}
