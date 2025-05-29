using System;
using Skill = SpaceCore.Skills.Skill;

namespace SwordAndSorcerySMAPI.Framework.ModSkills
{
    /// <summary>Construct an instance.</summary>
    /// <param name="skill">The parent skill.</param>
    /// <param name="id">The unique profession ID.</param>
    /// <param name="name">The translated profession name.</param>
    /// <param name="description">The translated profession description.</param>
    public class GenericProfession(Skill skill, string id, Func<string> name, Func<string> description) : Skill.Profession(skill, id)
    {
        /*********
        ** Fields
        *********/
        /// <summary>Get the translated profession name.</summary>
        private readonly Func<string> GetNameImpl = name;

        /// <summary>Get the translated profession name.</summary>
        private readonly Func<string> GetDescriptionImpl = description;


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public override string GetName()
        {
            return GetNameImpl();
        }

        /// <inheritdoc />
        public override string GetDescription()
        {
            return GetDescriptionImpl();
        }
    }
}
