-- Database of black-and-white nonograms that reward more than 2000 XP.
-- The reason color and B/W nonograms are split is because of the average low
-- amount of XP for B/W nonograms.
-- For adding nonograms that reward less than 2000 XP use "Module:Data/XP_nonograms_bw_other".
-- If a name uses double quotes use a forward slash (e.g. "The "Z" letter" -> "The \"Z\" letter").

-------------------------- ONLY BASE XP FROM NONOGRAMS! -------------------------- 
-- (Without any bonuses, like Fried eggs, Smoothie, Hat of concentration, etc.) --

-- ABOUT NEW XP values:
-- Please add the XP from game version 21.0 in the < new_xp = "", > line.
-- Only yellow and orange dots nonograms have been affected.
-- The values with ~ before are calculated but they are not checked yet.

local nonograms = {
	-- Placeholder (for copy-pasting).
	{
		link            = "",
		author          = "",
		xp              = "",
		new_xp          = "",
		size            = "",
		category_1      = "",
		category_2      = "",
		puzzle_type     = "",
	},
}

return nonograms
