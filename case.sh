#!/bin/bash

#My example case statement

echo " What is your favorite color?"
read COLOR 

case $COLOR in
	blue)
	    echo "Good choice, but not correct..."
	;;
	red)
	    echo "Red is just a bad choice, try again.."
	;;
	clear)
	    echo "Correct! It is hard to see but the best!"
	;;
        *)
	    echo "The choices are blue, red, or clear."	
esac	    
