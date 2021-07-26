// A circular progress bar 
const ProgressRing = Vue.component('progress-ring', {
    props: {
        radius: Number,
        progress: Number,
        stroke: Number,
        color: String
    },
    data() {
        const normalizedRadius = this.radius - this.stroke * 0.5;
        const circumference = normalizedRadius * 2 * Math.PI;

        return {
            normalizedRadius,
            circumference
        };
    },
    computed: {
        strokeDashoffset() {
            return this.circumference - this.progress / 100 * this.circumference;
        }
    },
    template: `
          <svg :viewBox="'0 0 ' + radius*2 + ' ' + radius*2" width="100%" height="100%">
              <circle
                  class="circular-progress-bar"
                  :stroke="color"
                  :stroke-dasharray="circumference + ' ' + circumference"
                  :style="{ strokeDashoffset: strokeDashoffset }"
                  :stroke-width="stroke"
                  fill="transparent"
                  :r="normalizedRadius"
                  :cx="radius"
                  :cy="radius"
              />
          </svg>`
});