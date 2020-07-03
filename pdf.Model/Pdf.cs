using Newtonsoft.Json;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JsonConverter = System.Text.Json.Serialization.JsonConverter;

namespace pdf.Model
{
    [JsonObject]
    public class Pdf : IValidatableObject
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [JsonProperty]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the title
        /// </summary>
        [MaxLength(256)]
        [JsonProperty]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the activation date
        /// </summary>
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:G}")]
        [JsonProperty]
        public DateTime UploadDate { get; set; }

        //[MaxLength(5242880)] // Better to have this in the "C" logic
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public byte[] Content { get; set; }

        [JsonProperty]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public long Size { get; set; }

        /// <summary>
        /// Supplier validation
        /// </summary>
        /// <param name="validationContext"><see cref="ValidationContext"/></param>
        /// <returns>List of <see cref="ValidationResult"/></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResults = new List<ValidationResult>();

            Validator.TryValidateProperty(this.Title, new ValidationContext(this, null, null) { MemberName = "Title" }, validationResults);
            Validator.TryValidateProperty(this.UploadDate, new ValidationContext(this, null, null) { MemberName = "UploadDate" }, validationResults);
            Validator.TryValidateProperty(this.Content, new ValidationContext(this, null, null) { MemberName = "Content" }, validationResults);

            return validationResults;
        }
    }
}