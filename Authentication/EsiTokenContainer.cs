﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntrepreneurEsiApi.Authentication;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace EntrepreneurCommon.Authentication
{
    /// <summary>
    /// A wrapper class containing all required fields and properties to perform token operations.
    /// Purpose of this class is the ability to store it persistently in a database or file, using it as a heart of information.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class EsiTokenContainer : IEsiTokenContainer
    {
    #region IEsiTokenResponse, IEsiTokenVerification

        [DisplayName("Character ID")]
        public int CharacterId { get; set; }

        [DisplayName("Character Name")]
        public string CharacterName { get; set; }

        [DisplayName("Owner Hash")]
        public string CharacterOwnerHash { get; set; }

        [DisplayName("Expiry Date")]
        public string ExpiresOn { get; set; }

        [DisplayName("Scopes")]
        public string Scopes { get; set; }

        [DisplayName("Token Type")]
        public string TokenType { get; set; }

        [DisplayName("Access Token (Stored)")]
        public string AccessToken { get; set; }

        [DisplayName("Expires in...")]
        public int ExpiresIn { get; set; }

        [DisplayName("Refresh Token")]
        public string RefreshToken { get; set; }

    #endregion

    #region Supplementary properties

        [DisplayName("Expires in...")]
        public TimeSpan ExpiresInAuto {
            get {
                var span = (DateTime.Parse(ExpiresOn) - DateTime.Now);
                if (span.TotalSeconds > 0) return span;
                else return default;
            }
        }

        [DisplayName("Requires refreshing?")]
        public EnumNeedsRefreshing RequiresRefreshing => NeedsRefreshing();

        public string UUID { get; set; }

        public void AssignUUID()
        {
            UUID = System.Guid.NewGuid().ToString();
        }
    #endregion


        [JsonIgnore]
        public EsiAuthClient AuthClient { get; set; }

        public EsiTokenContainer(IEsiTokenResponse tokenResponse, IEsiTokenVerification tokenVerification = null,
            EsiAuthClient client = null)
        {
            // Assign Unique Identifier
            AssignUUID();
            // Get all the important data.
            this.AccessToken = tokenResponse.AccessToken;
            this.ExpiresIn = tokenResponse.ExpiresIn;
            this.RefreshToken = tokenResponse.RefreshToken;
            this.TokenType = tokenResponse.TokenType;

            if (tokenVerification != null) {
                this.CharacterId = tokenVerification.CharacterId;
                this.CharacterName = tokenVerification.CharacterName;
                this.CharacterOwnerHash = tokenVerification.CharacterOwnerHash;
                this.ExpiresOn = tokenVerification.ExpiresOn;
                this.Scopes = tokenVerification.Scopes;
                this.TokenType = tokenVerification.TokenType;
            }

            if (AuthClient != null) this.AuthClient = AuthClient;
        }

        public static EsiTokenContainer operator +(EsiTokenContainer left, IEsiTokenResponse right)
        {
            left.AssignTokenResponse(right);
            return left;
        }

        public static EsiTokenContainer operator +(EsiTokenContainer left, IEsiTokenVerification right)
        {
            left.AssignTokenVerification(right);
            return left;
        }

        protected void AssignTokenResponse(IEsiTokenResponse response)
        {
            this.AccessToken = response.AccessToken;
            this.ExpiresIn = response.ExpiresIn;
            this.RefreshToken = response.RefreshToken;
            this.TokenType = response.TokenType;
        }

        protected void AssignTokenVerification(IEsiTokenVerification response)
        {
            this.CharacterId = response.CharacterId;
            this.CharacterName = response.CharacterName;
            this.CharacterOwnerHash = response.CharacterOwnerHash;
            this.ExpiresOn = response.ExpiresOn;
            this.Scopes = response.Scopes;
            this.TokenType = response.TokenType;
        }

        public EnumNeedsRefreshing NeedsRefreshing()
        {
            if (this.RefreshToken == null) return EnumNeedsRefreshing.Invalid;

            DateTime now = DateTime.UtcNow;
            DateTime exp = DateTime.Parse(this.ExpiresOn);

            switch (now > exp) {
                case true:
                    return EnumNeedsRefreshing.Yes;
                case false:
                    return EnumNeedsRefreshing.No;
                default:
                    return EnumNeedsRefreshing.No;
            }
        }

        /// <summary>
        /// Refresh token if needed.
        /// </summary>
        /// <returns></returns>
        public async Task Refresh(bool force = false, EsiAuthClient client = null)
        {
            EsiAuthClient _client;
            // If client was passed in param, use that, else use the client in AuthClient property.
            if (client != null) _client = client;
            else _client = AuthClient;

        #if DEBUG
            Debug.Assert(_client != null);
        #endif

            if (_client == null)
                throw new Exception("There is no EsiAuthClient assigned to the token container. Cannot refresh.");
            if (this.NeedsRefreshing() == EnumNeedsRefreshing.Yes || force) {
                this.AssignTokenResponse(await AuthClient.RequestAccessToken(this.RefreshToken));
                this.AssignTokenVerification(await AuthClient.RequestTokenVerification(this.RefreshToken));
            }
        }

        /// <summary>
        /// Get up-to-date access token, refreshing it if needed.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetAccessToken(EsiAuthClient client = null)
        {
            await Refresh(client: client);
            return this.AccessToken;
        }

        /// <summary>
        /// Checks whether the token contains specified scope.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public bool CheckScope(string scope)
        {
            if (this.Scopes.ToLower().Contains(scope.ToLower())) return true;
            return false;
        }
    }
}